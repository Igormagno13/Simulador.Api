// Simulacao/Data/StorageService.cs
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Simulador.Api.Simulacao.Models;
using System.Linq;

namespace Simulador.Api.Simulacao.Data;

public class StorageService
{
    private readonly string _dbPath;
    private readonly string _cs;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = null // manter nomes dos DTOs como estão
    };

    public StorageService()
    {
        _dbPath = Environment.GetEnvironmentVariable("LOCAL_DB_PATH") ?? "local.db";
        _cs = $"Data Source={_dbPath}";
    }

    private SqliteConnection Create() => new(_cs);

    // ------------------------------------------------------------------
    // SCHEMA
    // ------------------------------------------------------------------
    public void EnsureDb()
    {
        using var cx = Create();
        cx.Open();

        // simulacoes
        using (var cmd = cx.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS simulacoes (
                    id_simulacao      INTEGER PRIMARY KEY,
                    codigo_produto    INTEGER,
                    descricao_produto TEXT,
                    taxa_juros        REAL,
                    valor_desejado    REAL,
                    prazo             INTEGER,
                    envelope_json     TEXT NOT NULL,
                    created_at        TEXT DEFAULT (datetime('now')),
                    duration_ms       REAL
                );
            """;
            cmd.ExecuteNonQuery();
        }

        // telemetria (com sucesso_2xx/falha_n2xx)
        using (var cmd = cx.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS telemetria (
                    servico     TEXT PRIMARY KEY,
                    chamadas    INTEGER NOT NULL DEFAULT 0,
                    total_ms    REAL    NOT NULL DEFAULT 0,
                    min_ms      REAL,
                    max_ms      REAL,
                    sucesso_2xx INTEGER NOT NULL DEFAULT 0,
                    falha_n2xx  INTEGER NOT NULL DEFAULT 0
                );
            """;
            cmd.ExecuteNonQuery();
        }

        // migração defensiva: adiciona colunas se ainda não existirem
        TryAddColumn(cx, "telemetria", "sucesso_2xx", "INTEGER NOT NULL DEFAULT 0");
        TryAddColumn(cx, "telemetria", "falha_n2xx",  "INTEGER NOT NULL DEFAULT 0");
    }

    private static void TryAddColumn(SqliteConnection cx, string table, string column, string ddlType)
    {
        try
        {
            using var alter = cx.CreateCommand();
            alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {ddlType};";
            alter.ExecuteNonQuery();
        }
        catch
        {
            // coluna já existe – ignorar
        }
    }

    // ------------------------------------------------------------------
    // GRAVAÇÃO DE SIMULAÇÕES
    // ------------------------------------------------------------------
    public async Task SaveSimulacaoAsync(
        SimulacaoEnvelope envelope, decimal valorDesejado, int prazo, double durationMs)
    {
        await using var cx = Create();
        await cx.OpenAsync();

        var json = JsonSerializer.Serialize(envelope, JsonOpts);

        await using var cmd = cx.CreateCommand();
        cmd.CommandText = """
            INSERT INTO simulacoes
                (id_simulacao, codigo_produto, descricao_produto,
                 taxa_juros, valor_desejado, prazo, envelope_json, duration_ms)
            VALUES
                ($id, $cod, $desc, $taxa, $valor, $prazo, $env, $dur);
        """;
        cmd.Parameters.AddWithValue("$id",   envelope.IdSimulacao);
        cmd.Parameters.AddWithValue("$cod",  envelope.CodigoProduto);
        cmd.Parameters.AddWithValue("$desc", envelope.DescricaoProduto);
        cmd.Parameters.AddWithValue("$taxa", envelope.TaxaJuros);
        cmd.Parameters.AddWithValue("$valor", valorDesejado);
        cmd.Parameters.AddWithValue("$prazo", prazo);
        cmd.Parameters.AddWithValue("$env",  json);
        cmd.Parameters.AddWithValue("$dur",  durationMs);
        await cmd.ExecuteNonQueryAsync();
    }

    // ------------------------------------------------------------------
    // 5) LISTAGEM PAGINADA (modelo do hackathon)
    // ------------------------------------------------------------------
    public ListaSimulacoesDto GetSimulacoesPaginadas(int pagina, int qtdRegistrosPagina)
    {
        if (pagina < 1) pagina = 1;
        if (qtdRegistrosPagina < 1) qtdRegistrosPagina = 1;

        using var cx = Create();
        cx.Open();

        int total;
        using (var c = cx.CreateCommand())
        {
            c.CommandText = "SELECT COUNT(*) FROM simulacoes;";
            total = Convert.ToInt32(c.ExecuteScalar());
        }

        int skip = (pagina - 1) * qtdRegistrosPagina;

        using var cmd = cx.CreateCommand();
        cmd.CommandText = """
            SELECT id_simulacao, valor_desejado, prazo, envelope_json
              FROM simulacoes
          ORDER BY id_simulacao DESC
             LIMIT $take OFFSET $skip;
        """;
        cmd.Parameters.AddWithValue("$take", qtdRegistrosPagina);
        cmd.Parameters.AddWithValue("$skip", skip);

        using var rd = cmd.ExecuteReader();
        var regs = new List<SimulacaoResumoDto>();

        while (rd.Read())
        {
            long id = rd.GetInt64(0);
            decimal valor = rd.GetDecimal(1);
            int prazo = rd.GetInt32(2);
            string envJson = rd.GetString(3);

            var env = JsonSerializer.Deserialize<SimulacaoEnvelope>(envJson, JsonOpts)!;

            decimal totalParcelas = env.ResultadoSimulacao
                .SelectMany(x => x.Parcelas)
                .Sum(p => p.ValorPrestacao);

            regs.Add(new SimulacaoResumoDto(
                IdSimulacao: id,
                ValorDesejado: decimal.Round(valor, 2),
                Prazo: prazo,
                ValorTotalParcelas: decimal.Round(totalParcelas, 2)
            ));
        }

        return new ListaSimulacoesDto(
            Pagina: pagina,
            QtdRegistros: total,
            QtdRegistrosPagina: qtdRegistrosPagina,
            Registros: regs
        );
    }

    // ------------------------------------------------------------------
    // 6) VOLUME POR PRODUTO/DIA
    // ------------------------------------------------------------------
    public VolumePorProdutoDiaDto GetVolumePorProdutoDia(DateOnly data)
    {
        string dataStr = data.ToString("yyyy-MM-dd");

        using var cx = Create();
        cx.Open();

        using var cmd = cx.CreateCommand();
        cmd.CommandText = """
            SELECT codigo_produto, descricao_produto, taxa_juros, valor_desejado, envelope_json
              FROM simulacoes
             WHERE date(created_at) = $d;
        """;
        cmd.Parameters.AddWithValue("$d", dataStr);

        using var rd = cmd.ExecuteReader();
        var bucket = new Dictionary<int, List<(decimal taxa, decimal valorDesejado, decimal totalParcelas, string desc)>>();

        while (rd.Read())
        {
            int cod = rd.GetInt32(0);
            string desc = rd.GetString(1);
            decimal taxa = rd.GetDecimal(2);
            decimal valor = rd.GetDecimal(3);
            string envJson = rd.GetString(4);

            var env = JsonSerializer.Deserialize<SimulacaoEnvelope>(envJson, JsonOpts)!;

            decimal totalParcelas = env.ResultadoSimulacao
                .SelectMany(x => x.Parcelas)
                .Sum(p => p.ValorPrestacao);

            if (!bucket.TryGetValue(cod, out var list))
                bucket[cod] = list = new();

            list.Add((taxa, valor, totalParcelas, desc));
        }

        var itens = new List<VolumeProdutoItemDto>();
        foreach (var kv in bucket)
        {
            int cod = kv.Key;
            var arr = kv.Value;
            string desc = arr[0].desc;

            decimal taxaMedia = arr.Average(x => x.taxa);
            decimal mediaPrestacao = arr.Average(x => x.totalParcelas);
            decimal totalDesejado = arr.Sum(x => x.valorDesejado);
            decimal totalCredito = arr.Sum(x => x.totalParcelas);

            itens.Add(new VolumeProdutoItemDto(
                CodigoProduto: cod,
                DescricaoProduto: desc,
                TaxaMediaJuro: decimal.Round(taxaMedia, 6),
                ValorMedioPrestacao: decimal.Round(mediaPrestacao, 2),
                ValorTotalDesejado: decimal.Round(totalDesejado, 2),
                ValorTotalCredito: decimal.Round(totalCredito, 2)
            ));
        }

        return new VolumePorProdutoDiaDto(
            DataReferencia: dataStr,
            Simulacoes: itens
        );
    }

    // ------------------------------------------------------------------
    // 7) TELEMETRIA (registra sucesso/erro e reporta no formato pedido)
    // ------------------------------------------------------------------
    public async Task RecordTelemetryAsync(string servico, double durationMs, int status)
    {
        var isOk = status >= 200 && status < 300;

        await using var cx = Create();
        await cx.OpenAsync();

        // tenta UPDATE; se não existir, faz INSERT
        await using (var up = cx.CreateCommand())
        {
            up.CommandText = """
                UPDATE telemetria
                   SET chamadas   = chamadas + 1,
                       total_ms   = total_ms + $d,
                       min_ms     = CASE WHEN min_ms IS NULL OR $d < min_ms THEN $d ELSE min_ms END,
                       max_ms     = CASE WHEN max_ms IS NULL OR $d > max_ms THEN $d ELSE max_ms END,
                       sucesso_2xx = sucesso_2xx + $ok,
                       falha_n2xx  = falha_n2xx  + $ko
                 WHERE servico = $s;
            """;
            up.Parameters.AddWithValue("$d",  durationMs);
            up.Parameters.AddWithValue("$s",  servico);
            up.Parameters.AddWithValue("$ok", isOk ? 1 : 0);
            up.Parameters.AddWithValue("$ko", isOk ? 0 : 1);

            var rows = await up.ExecuteNonQueryAsync();
            if (rows == 0)
            {
                await using var ins = cx.CreateCommand();
                ins.CommandText = """
                    INSERT INTO telemetria
                        (servico, chamadas, total_ms, min_ms, max_ms, sucesso_2xx, falha_n2xx)
                    VALUES ($s, 1, $d, $d, $d, $ok, $ko);
                """;
                ins.Parameters.AddWithValue("$s",  servico);
                ins.Parameters.AddWithValue("$d",  durationMs);
                ins.Parameters.AddWithValue("$ok", isOk ? 1 : 0);
                ins.Parameters.AddWithValue("$ko", isOk ? 0 : 1);
                await ins.ExecuteNonQueryAsync();
            }
        }
    }

    public TelemetryReportDto GetTelemetryReport(DateOnly data)
    {
        using var cx = Create();
        cx.Open();

        using var cmd = cx.CreateCommand();
        cmd.CommandText = """
            SELECT servico, chamadas, total_ms, min_ms, max_ms, sucesso_2xx
              FROM telemetria
          ORDER BY servico;
        """;

        using var rd = cmd.ExecuteReader();
        var itens = new List<EndpointTelemetryDto>();

        while (rd.Read())
        {
            string serv  = rd.GetString(0);
            int chamadas = rd.GetInt32(1);
            double total = rd.IsDBNull(2) ? 0.0 : rd.GetDouble(2);
            double min   = rd.IsDBNull(3) ? 0.0 : rd.GetDouble(3);
            double max   = rd.IsDBNull(4) ? 0.0 : rd.GetDouble(4);
            int sucesso2xx = rd.GetInt32(5);

            double avg  = chamadas > 0 ? total / chamadas : 0.0;
            double perc = chamadas > 0 ? (double)sucesso2xx / chamadas : 0.0;

            itens.Add(new EndpointTelemetryDto(
                NomeApi: serv,
                QtdRequisicoes: chamadas,
                TempoMedio: Math.Round(avg, 2),
                TempoMinimo: Math.Round(min, 2),
                TempoMaximo: Math.Round(max, 2),
                PercentualSucesso: Math.Round(perc, 4)
            ));
        }

        return new TelemetryReportDto(
            DataReferencia: data.ToString("yyyy-MM-dd"),
            ListaEndpoints: itens
        );
    }

    // ------------------------------------------------------------------
    // Helpers “legados” (mantidos para compatibilidade)
    // ------------------------------------------------------------------
    public async Task<List<object>> ListSimulacoesAsync(int limit = 100)
    {
        var outList = new List<object>();
        await using var cx = Create();
        await cx.OpenAsync();

        await using var cmd = cx.CreateCommand();
        cmd.CommandText = """
            SELECT id_simulacao, codigo_produto, descricao_produto, taxa_juros,
                   valor_desejado, prazo, created_at, duration_ms, envelope_json
              FROM simulacoes
          ORDER BY id_simulacao DESC
             LIMIT $lim;
        """;
        cmd.Parameters.AddWithValue("$lim", limit);

        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            var id   = rd.GetInt64(0);
            var cod  = rd.IsDBNull(1) ? (long?)null : rd.GetInt64(1);
            var desc = rd.IsDBNull(2) ? "" : rd.GetString(2);
            var taxa = rd.IsDBNull(3) ? 0m : rd.GetDecimal(3);
            var val  = rd.IsDBNull(4) ? 0m : rd.GetDecimal(4);
            var pr   = rd.IsDBNull(5) ? 0 : rd.GetInt32(5);
            var created = rd.IsDBNull(6) ? "" : rd.GetString(6);
            var dur  = rd.IsDBNull(7) ? 0.0 : rd.GetDouble(7);
            var env  = rd.IsDBNull(8) ? "{}" : rd.GetString(8);

            outList.Add(new {
                idSimulacao      = id,
                codigoProduto    = cod,
                descricaoProduto = desc,
                taxaJuros        = taxa,
                valorDesejado    = val,
                prazo            = pr,
                createdAt        = created,
                durationMs       = Math.Round(dur, 2),
                envelope         = JsonSerializer.Deserialize<object>(env, JsonOpts)
            });
        }
        return outList;
    }

    public async Task<List<object>> GetTelemetryAsync()
    {
        var outList = new List<object>();
        await using var cx = Create();
        await cx.OpenAsync();

        await using var cmd = cx.CreateCommand();
        cmd.CommandText = """
            SELECT servico, chamadas, total_ms, min_ms, max_ms, sucesso_2xx
              FROM telemetria
          ORDER BY servico;
        """;

        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            var serv  = rd.GetString(0);
            var cham  = rd.GetInt32(1);
            var total = rd.IsDBNull(2) ? 0.0 : rd.GetDouble(2);
            var min   = rd.IsDBNull(3) ? 0.0 : rd.GetDouble(3);
            var max   = rd.IsDBNull(4) ? 0.0 : rd.GetDouble(4);
            var ok2xx = rd.GetInt32(5);
            var avg   = cham > 0 ? total / cham : 0.0;
            var perc  = cham > 0 ? (double)ok2xx / cham : 0.0;

            outList.Add(new {
                servico      = serv,
                chamadas     = cham,
                avgMs        = Math.Round(avg, 2),
                minMs        = Math.Round(min, 2),
                maxMs        = Math.Round(max, 2),
                totalMs      = Math.Round(total, 2),
                sucesso_2xx  = ok2xx,
                percSucesso  = Math.Round(perc, 4)
            });
        }
        return outList;
    }
}
