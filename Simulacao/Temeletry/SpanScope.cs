// Simulacao/Telemetry/SpanScope.cs

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using static Simulador.Api.Simulacao.Telemetry.RequestTelemetryMiddleware;

using Simulador.Api.Simulacao.Data;

namespace Simulador.Api.Simulacao.Telemetry;

/// <summary>
/// Uso:
/// using var span = new SpanScope("db_obter_produto", logger, httpCtxAccessor, storage);
/// </summary>
public sealed class SpanScope : IDisposable
{
    private readonly string _name;
    private readonly ILogger _logger;
    private readonly IHttpContextAccessor _ctx;
    private readonly StorageService? _storage;
    private readonly Stopwatch _sw = Stopwatch.StartNew();

    public SpanScope(string name, ILogger logger, IHttpContextAccessor ctx, StorageService? storage = null)
    {
        _name    = name;
        _logger  = logger;
        _ctx     = ctx;
        _storage = storage;
    }

    public void Dispose()
    {
        _sw.Stop();
        var dur = Math.Round(_sw.Elapsed.TotalMilliseconds, 2);

        var correlationId = _ctx.HttpContext?.Items[CorrelationItemKey]?.ToString();

        // log estruturado (JSON), equivalente ao Python
        _logger.LogInformation("span {@data}", new
        {
            msg = "span",
            span = _name,
            duration_ms = dur,
            correlation_id = correlationId
        });

        // opcional: agrega na tabela de telemetria (igual ao Python)
        // Como é um span interno (não um request), marcamos como sucesso (200).
        if (_storage is not null)
        {
            _ = _storage.RecordTelemetryAsync(_name, dur, status: 200); // fire-and-forget
        }
    }
}
