//Simulacao/Models/ListaSimulacoesDto.cs

namespace Simulador.Api.Simulacao.Models;

public record SimulacaoResumoDto(
    long IdSimulacao,
    decimal ValorDesejado,
    int Prazo,
    decimal ValorTotalParcelas
);

public record ListaSimulacoesDto(
    int Pagina,
    int QtdRegistros,
    int QtdRegistrosPagina,
    List<SimulacaoResumoDto> Registros
);
