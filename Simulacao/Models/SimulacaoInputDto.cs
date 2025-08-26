// Simulacao/Models/SimulacaoInputDto.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Simulador.Api.Simulacao.Models;

public class SimulacaoInputDto
{
    // Valor mínimo 200 (decimal) — evita conversão para double
    [Range(typeof(decimal),
           "200",
           "79228162514264337593543950335",   // decimal.MaxValue
           ErrorMessage = "O valor mínimo é 200.")]
    [DefaultValue(typeof(decimal), "300")]      // opcional: aparece no Swagger
    public decimal ValorDesejado { get; set; }

    // 0..96 meses
    [Range(0, 96, ErrorMessage = "Prazo deve estar entre 0 e 96 meses.")]
    [DefaultValue(12)]                           // opcional: exemplo no Swagger
    public int Prazo { get; set; }
}

