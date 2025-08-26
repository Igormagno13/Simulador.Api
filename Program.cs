// Program.cs

using Microsoft.Data.SqlClient;
using Simulador.Api;                        // SqlConnectionFactory
using Simulador.Api.Simulacao.Data;         // ProdutoRepository, StorageService
using Simulador.Api.Simulacao.Endpoints;    // MapProduto(), MapTabelas(), MapSimulador(), MapStorage()
using Simulador.Api.Simulacao.Telemetry;    // UseRequestTelemetry()

var builder = WebApplication.CreateBuilder(args);

// JSON: manter nomes exatos dos DTOs (sem camelCase)
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = null;
    o.SerializerOptions.DictionaryKeyPolicy = null;
});

// DI
builder.Services.AddSingleton<SqlConnectionFactory>(); // SQL Server (para health)
builder.Services.AddScoped<ProdutoRepository>();        // Repositório (SQL Server)
builder.Services.AddSingleton<StorageService>();        // SQLite local p/ storage/telemetria
builder.Services.AddSingleton<EventHubPublisher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// LOG JSON no console
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

// Necessário para correlation-id no span/middleware
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Cria/garante tabelas do SQLite local
app.Services.GetRequiredService<StorageService>().EnsureDb();

// Telemetria de request (correlation-id + duração)
app.UseRequestTelemetry();

// Swagger (dev)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// (opcional) redirecionar raiz → Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// Health: SELECT 1 no SQL Server
app.MapGet("/health/db", async (SqlConnectionFactory factory) =>
{
    await using SqlConnection con = factory.Create();
    await con.OpenAsync();

    await using SqlCommand cmd = con.CreateCommand();
    cmd.CommandText = "SELECT 1";
    var result = await cmd.ExecuteScalarAsync();

    return Results.Ok(new { db = "ok", result });
});

// Endpoints de negócio
app.MapProduto();    // /api/produtos/localizar
app.MapTabelas();    // /api/tabelas/price e /api/tabelas/sac
app.MapSimulador();  // /api/simulacao
app.MapStorage();    // /api/storage/simulacoes e /api/storage/telemetria

app.Run();
