// Simulacao/Data/EventHubPublisher.cs
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Simulador.Api.Simulacao.Data;

public sealed class EventHubPublisher
{
    private readonly string? _connectionString;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = null
    };

    public EventHubPublisher(IConfiguration cfg)
    {
        // pode vir de appsettings.json (EventHub:ConnectionString) ou variável de ambiente
        _connectionString = cfg["EventHub:ConnectionString"];
    }

    public bool Enabled => !string.IsNullOrWhiteSpace(_connectionString);

    public async Task PublishAsync<T>(T payload, CancellationToken ct = default)
    {
        if (!Enabled) return;

        var json = JsonSerializer.Serialize(payload, JsonOpts);

        await using var producer = new EventHubProducerClient(_connectionString);

        // cria um batch e adiciona o evento (sem "using" em EventData)
        using EventDataBatch batch = await producer.CreateBatchAsync(ct);
        if (!batch.TryAdd(new EventData(BinaryData.FromString(json))))
            throw new InvalidOperationException("Evento excede o tamanho máximo de um batch.");

        await producer.SendAsync(batch, ct);
    }
}

