//Simulacao/Models/ParcelaDto.cs

using System.Text.Json.Serialization;

namespace Simulador.Api.Simulacao.Models;

public class ParcelaDto
{
    [JsonPropertyOrder(1)] public int     Numero            { get; set; }
    [JsonPropertyOrder(2)] public decimal ValorAmortizacao  { get; set; }
    [JsonPropertyOrder(3)] public decimal ValorJuros        { get; set; }
    [JsonPropertyOrder(4)] public decimal ValorPrestacao    { get; set; }
}
