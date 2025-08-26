// Simulacao/Endpoints/ProdutoEndpoints.cs
using Simulador.Api.Simulacao.Data;

namespace Simulador.Api.Simulacao.Endpoints;

public static class ProdutoEndpoints
{
    public static IEndpointRouteBuilder MapProduto(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/produtos");

        g.MapGet("localizar", async (decimal valorDesejado, int prazo, ProdutoRepository repo, CancellationToken ct) =>
        {
            var p = await repo.ObterProdutoAsync(valorDesejado, prazo, ct);
            return p is null ? Results.NotFound() : Results.Ok(p);
        });

        return app;
    }
}
