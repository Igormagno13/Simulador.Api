// Simulacao/Telemetry/RequestTelemetryMiddleware.cs
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Simulador.Api.Simulacao.Data;

namespace Simulador.Api.Simulacao.Telemetry;

public class RequestTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTelemetryMiddleware> _logger;
    private readonly StorageService _storage;

    public const string CorrelationHeader = "X-Correlation-ID";
    public const string CorrelationItemKey = "CorrelationId";

    public RequestTelemetryMiddleware(
        RequestDelegate next,
        ILogger<RequestTelemetryMiddleware> logger,
        StorageService storage)
    {
        _next = next;
        _logger = logger;
        _storage = storage;
    }

    public async Task Invoke(HttpContext ctx)
    {
        var sw = Stopwatch.StartNew();

        // correlation-id: usa o header ou cria um novo
        var correlationId =
            (ctx.Request.Headers.TryGetValue(CorrelationHeader, out var v) && !string.IsNullOrWhiteSpace(v))
                ? v.ToString()
                : Guid.NewGuid().ToString();

        ctx.Items[CorrelationItemKey] = correlationId;
        ctx.Response.OnStarting(() =>
        {
            ctx.Response.Headers[CorrelationHeader] = correlationId;
            return Task.CompletedTask;
        });

        int status = 0;

        try
        {
            await _next(ctx);
            status = ctx.Response?.StatusCode ?? 0;
        }
        catch (Exception ex)
        {
            // garante registro como erro (5xx) e repropaga
            status = StatusCodes.Status500InternalServerError;
            _logger.LogError(ex, "Unhandled exception while processing {Path}", ctx.Request.Path);
            throw;
        }
        finally
        {
            sw.Stop();
            var durationMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2);

            var routeName = GetRouteName(ctx);

            // 1) grava na tabela de telemetria (inclui sucesso/erro)
            // sucesso = status 2xx; os demais contam como falha
            try
            {
                await _storage.RecordTelemetryAsync(routeName, durationMs, status);
            }
            catch (Exception e)
            {
                // não deixar telemetria derrubar a request
                _logger.LogWarning(e, "Falha ao registrar telemetria para {Route}", routeName);
            }

            // 2) log estruturado ao console (JSON)
            _logger.LogInformation("http_request {@data}", new
            {
                msg = "http_request",
                route = routeName,
                method = ctx.Request.Method,
                status,
                duration_ms = durationMs,
                correlation_id = correlationId,
                path = ctx.Request.Path.ToString()
            });
        }
    }

    private static string GetRouteName(HttpContext ctx)
    {
        var ep = ctx.GetEndpoint();

        // Tente pegar a rota “bonita” do endpoint
        if (ep is RouteEndpoint rep)
        {
            var raw = rep.RoutePattern?.RawText;
            if (!string.IsNullOrWhiteSpace(raw)) return raw!;
        }

        // Fallbacks
        return ep?.DisplayName ?? ctx.Request.Path.ToString();
    }
}
