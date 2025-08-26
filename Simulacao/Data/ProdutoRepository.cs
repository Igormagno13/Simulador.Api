// Simulacao/Data/ProdutoRepository.cs

using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Simulador.Api.Simulacao.Models;
using Simulador.Api.Simulacao.Telemetry;

namespace Simulador.Api.Simulacao.Data;

public class ProdutoRepository
{
    private readonly string _cs;
    private readonly ILogger<ProdutoRepository> _logger;
    private readonly IHttpContextAccessor _http;
    private readonly StorageService _storage;

    public ProdutoRepository(
        IConfiguration cfg,
        ILogger<ProdutoRepository> logger,
        IHttpContextAccessor http,
        StorageService storage)
    {
        _cs = cfg.GetConnectionString("SqlServer")
              ?? throw new InvalidOperationException("ConnectionStrings:SqlServer não configurada.");
        _logger = logger;
        _http = http;
        _storage = storage;
    }

    /// <summary>
    /// Busca 1 produto que satisfaça os limites de prazo e valor.
    /// Prioriza o mais específico (maior mínimo compatível).
    /// </summary>
    public async Task<ProdutoDto?> ObterProdutoAsync(decimal valorDesejado, int prazo, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP 1
                CO_PRODUTO, NO_PRODUTO, PC_TAXA_JUROS,
                NU_MINIMO_MESES, NU_MAXIMO_MESES,
                VR_MINIMO, VR_MAXIMO
            FROM dbo.Produto
            WHERE
                @prazo >= NU_MINIMO_MESES
                AND (NU_MAXIMO_MESES IS NULL OR @prazo <= NU_MAXIMO_MESES)
                AND @valor >= VR_MINIMO
                AND (VR_MAXIMO IS NULL OR @valor <= VR_MAXIMO)
            ORDER BY
                NU_MINIMO_MESES DESC,
                VR_MINIMO DESC;
        """;

        // mede o tempo do acesso ao banco e grava log + telemetria agregada (SQLite)
        using var span = new SpanScope("db_obter_produto", _logger, _http, _storage);

        await using var con = new SqlConnection(_cs);
        await con.OpenAsync(ct);

        var prod = await con.QueryFirstOrDefaultAsync<ProdutoDto>(
            new CommandDefinition(
                sql,
                new { prazo, valor = valorDesejado },
                cancellationToken: ct));

        return prod;
    }
}
