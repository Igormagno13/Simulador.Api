//Simulacao/Models/SimulacaoEnvelope.cs

using System.Text.Json.Serialization;

namespace Simulador.Api.Simulacao.Models;

public class ResultadoSimulacaoOut
{
    [JsonPropertyOrder(1)] public string Tipo { get; set; } = "";       // "SAC" | "PRICE"
    [JsonPropertyOrder(2)] public List<ParcelaDto> Parcelas { get; set; } = new();
}

public class SimulacaoEnvelope
{
    [JsonPropertyOrder(1)] public long   IdSimulacao       { get; set; }
    [JsonPropertyOrder(2)] public int    CodigoProduto     { get; set; }
    [JsonPropertyOrder(3)] public string DescricaoProduto  { get; set; } = "";
    [JsonPropertyOrder(4)] public decimal TaxaJuros        { get; set; } // 6 casas
    [JsonPropertyOrder(5)] public List<ResultadoSimulacaoOut> ResultadoSimulacao { get; set; } = new();
}
