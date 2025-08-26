//Simulacao/Models/VolumePorProdutoDto.cs

namespace Simulador.Api.Simulacao.Models;

public record VolumeProdutoItemDto(
    int CodigoProduto,
    string DescricaoProduto,
    decimal TaxaMediaJuro,
    decimal ValorMedioPrestacao,
    decimal ValorTotalDesejado,
    decimal ValorTotalCredito
);

public record VolumePorProdutoDiaDto(
    string DataReferencia,                 // "YYYY-MM-DD"
    List<VolumeProdutoItemDto> Simulacoes
);
