//Simulacao/Endpoints/TabelasEndpoints.cs

using Simulador.Api.Simulacao.Calculo;

namespace Simulador.Api.Simulacao.Endpoints;

public static class TabelasEndpoints
{
    public static IEndpointRouteBuilder MapTabelas(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/tabelas");

        // GET /api/tabelas/price?valorDesejado=1000&prazo=12&taxa=0.02
        g.MapGet("price", (decimal valorDesejado, int prazo, decimal taxa) =>
        {
            var parcelas = Tabelas.TabelaPrice(valorDesejado, prazo, taxa);
            return Results.Ok(parcelas);
        });

        // GET /api/tabelas/sac?valorDesejado=1000&prazo=12&taxa=0.02
        g.MapGet("sac", (decimal valorDesejado, int prazo, decimal taxa) =>
        {
            var parcelas = Tabelas.TabelaSac(valorDesejado, prazo, taxa);
            return Results.Ok(parcelas);
        });

        return app;
    }
}
