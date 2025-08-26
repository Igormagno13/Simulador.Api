// Simulacao/Endpoints/SimuladorEndpoints.cs
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Simulador.Api.Simulacao.Calculo;
using Simulador.Api.Simulacao.Data;
using Simulador.Api.Simulacao.Models;

namespace Simulador.Api.Simulacao.Endpoints;

public static class SimuladorEndpoints
{
    private static long GerarIdSimulacao()
        => long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

    public static IEndpointRouteBuilder MapSimulador(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/simulacao").WithTags("Simulador.Api");

        
        g.MapGet("",
            async ([FromQuery] decimal valorDesejado,
                   [FromQuery] int prazo,
                   ProdutoRepository repo,
                   StorageService storage,
                   EventHubPublisher publisher,
                   CancellationToken ct) =>
            {
                var result = await RodarSimulacao(valorDesejado, prazo, repo, storage, publisher, ct);
                return result;
            })
         .WithName("GetSimulacao")
         .Produces<SimulacaoEnvelope>(StatusCodes.Status200OK)
         .Produces(StatusCodes.Status404NotFound)
         .WithSummary("Executa a simulação (query string)")
         .WithDescription("Parâmetros: valorDesejado (decimal) e prazo (int).");

        
        g.MapPost("",
            async ([FromBody] SimulacaoInputDto input,
                   ProdutoRepository repo,
                   StorageService storage,
                   EventHubPublisher publisher,
                   CancellationToken ct) =>
            {
                if (input is null)
                    return Results.BadRequest(new { erro = "Body inválido." });

                return await RodarSimulacao(input.ValorDesejado, input.Prazo, repo, storage, publisher, ct);
            })
         .WithName("PostSimulacao")
         .Accepts<SimulacaoInputDto>("application/json")
         .Produces<SimulacaoEnvelope>(StatusCodes.Status200OK)
         .Produces(StatusCodes.Status400BadRequest)
         .Produces(StatusCodes.Status404NotFound)
         .WithSummary("Executa a simulação (POST)")
         .WithDescription("Body JSON: { \"valorDesejado\": number, \"prazo\": int }");

        return app;
    }

    private static async Task<IResult> RodarSimulacao(
        decimal valorDesejado,
        int prazo,
        ProdutoRepository repo,
        StorageService storage,
        EventHubPublisher publisher,
        CancellationToken ct)
    {
        // mede duração apenas para salvar no SQLite (request timing é do middleware)
        var sw = Stopwatch.StartNew();

        var produto = await repo.ObterProdutoAsync(valorDesejado, prazo, ct);
        if (produto is null)
        {
            return Results.NotFound(new
            {
                erro = "Nenhum produto atende aos critérios.",
                valorDesejado,
                prazo
            });
        }

        var taxa = produto.PC_TAXA_JUROS;

        var sac   = Tabelas.TabelaSac(valorDesejado, prazo, taxa);
        var price = Tabelas.TabelaPrice(valorDesejado, prazo, taxa);

        var env = new SimulacaoEnvelope
        {
            IdSimulacao        = GerarIdSimulacao(),
            CodigoProduto      = produto.CO_PRODUTO,
            DescricaoProduto   = produto.NO_PRODUTO,
            TaxaJuros          = Math.Round(taxa, 6, MidpointRounding.ToEven),
            ResultadoSimulacao = new()
            {
                new ResultadoSimulacaoOut { Tipo = "SAC",   Parcelas = sac   },
                new ResultadoSimulacaoOut { Tipo = "PRICE", Parcelas = price }
            }
        };

        sw.Stop();
        await storage.SaveSimulacaoAsync(env, valorDesejado, prazo, sw.Elapsed.TotalMilliseconds);

        // publica no Event Hub (se configurado); não falha a request se der erro
        try { await publisher.PublishAsync(env, ct); } catch { /* log opcional */ }

        return Results.Ok(env);
    }
}
