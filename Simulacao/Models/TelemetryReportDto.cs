//Simulacao/Models/TelemetryReportDto.cs

namespace Simulador.Api.Simulacao.Models;

public record EndpointTelemetryDto(
    string NomeApi,
    int QtdRequisicoes,
    double TempoMedio,
    double TempoMinimo,
    double TempoMaximo,
    double PercentualSucesso
);

public record TelemetryReportDto(
    string DataReferencia,                 // "YYYY-MM-DD"
    List<EndpointTelemetryDto> ListaEndpoints
);
