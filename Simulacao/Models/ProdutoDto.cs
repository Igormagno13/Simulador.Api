// Simulacao/Models/ProdutoDto.cs
using System.Text.Json.Serialization;

namespace Simulador.Api.Simulacao.Models;

public class ProdutoDto
{
    [JsonPropertyOrder(1)] public int    CO_PRODUTO       { get; set; }
    [JsonPropertyOrder(2)] public string NO_PRODUTO       { get; set; } = "";
    [JsonPropertyOrder(3)] public decimal PC_TAXA_JUROS   { get; set; }
    [JsonPropertyOrder(4)] public int    NU_MINIMO_MESES  { get; set; }
    [JsonPropertyOrder(5)] public int?   NU_MAXIMO_MESES  { get; set; }  // pode ser NULL
    [JsonPropertyOrder(6)] public decimal VR_MINIMO       { get; set; }
    [JsonPropertyOrder(7)] public decimal? VR_MAXIMO      { get; set; }  // pode ser NULL
}
