// Simulacao/Telemetry/TelemetryExtensions.cs
using Microsoft.AspNetCore.Builder;

namespace Simulador.Api.Simulacao.Telemetry;

public static class TelemetryExtensions
{
    /// <summary>
    /// Adiciona o middleware que mede cada request, inclui o X-Correlation-ID
    /// e registra sucesso/erro no StorageService.
    /// </summary>
    public static IApplicationBuilder UseRequestTelemetry(this IApplicationBuilder app)
        => app.UseMiddleware<RequestTelemetryMiddleware>();
}
