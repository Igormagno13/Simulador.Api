// Simulacao/Endpoints/StorageEndpoints.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Simulador.Api.Simulacao.Data;
using Simulador.Api.Simulacao.Models;

namespace Simulador.Api.Simulacao.Endpoints;

public static class StorageEndpoints
{
    public static IEndpointRouteBuilder MapStorage(this IEndpointRouteBuilder app)
    {
        // 5) Listagem paginada no formato do hackathon
        app.MapGet("/api/storage/simulacoes",
            (StorageService storage, int pagina = 1, int qtdRegistrosPagina = 200) =>
            {
                var dto = storage.GetSimulacoesPaginadas(pagina, qtdRegistrosPagina);
                return Results.Ok(dto);
            })
            .WithName("ListarSimulacoes")
            .WithTags("Storage")
            .Produces<ListaSimulacoesDto>(StatusCodes.Status200OK);

        // 6) Volume por produto no dia
        app.MapGet("/api/storage/volume-por-produto",
            (StorageService storage, DateOnly? data) =>
            {
                var d = data ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
                var dto = storage.GetVolumePorProdutoDia(d);
                return Results.Ok(dto);
            })
            .WithName("VolumePorProdutoNoDia")
            .WithTags("Storage")
            .Produces<VolumePorProdutoDiaDto>(StatusCodes.Status200OK);

        // 7) Telemetria formatada
        app.MapGet("/api/storage/telemetria",
            (StorageService storage, DateOnly? data) =>
            {
                var d = data ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
                var dto = storage.GetTelemetryReport(d);
                return Results.Ok(dto);
            })
            .WithName("RelatorioDeTelemetria")
            .WithTags("Storage")
            .Produces<TelemetryReportDto>(StatusCodes.Status200OK);

        return app;
    }
}
