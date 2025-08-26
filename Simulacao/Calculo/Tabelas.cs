//Simulacao/Calculo/Tabelas.cs

using Simulador.Api.Simulacao.Models;

namespace Simulador.Api.Simulacao.Calculo;

public static class Tabelas
{
    // >>> ROUND(2) igual ao Python: ties-to-even
    private static decimal R2(decimal x) => Math.Round(x, 2, MidpointRounding.ToEven);

    /// <summary>
    /// PRICE - parcelas fixas
    /// taxa (t) em forma decimal (ex.: 0.02 = 2% a.m.)
    /// </summary>
    public static List<ParcelaDto> TabelaPrice(decimal valorDesejado, int prazo, decimal taxa)
    {
        var t = taxa;
        var v = valorDesejado;
        var n = prazo;

        decimal prestacao;
        if (t == 0)
        {
            prestacao = v / n;
        }
        else
        {
            // v * t / (1 - (1 + t)^(-n))
            var fator = 1 - (decimal)Math.Pow((double)(1 + t), -n);
            prestacao = v * t / fator;
        }

        decimal saldo = v;
        var parcelas = new List<ParcelaDto>(n);

        for (int i = 1; i <= n; i++)
        {
            var juros = saldo * t;
            var amort = prestacao - juros;

            decimal prest;
            if (i == n)
            {
                amort = saldo;             // zera saldo na última
                prest = amort + juros;
            }
            else
            {
                prest = prestacao;
            }

            saldo -= amort;

            parcelas.Add(new ParcelaDto
            {
                Numero            = i,
                ValorAmortizacao  = R2(amort),
                ValorJuros        = R2(juros),
                ValorPrestacao    = R2(prest)
            });
        }

        return parcelas;
    }

    /// <summary>
    /// SAC - amortização constante
    /// taxa (t) em forma decimal (ex.: 0.02 = 2% a.m.)
    /// </summary>
    public static List<ParcelaDto> TabelaSac(decimal valorDesejado, int prazo, decimal taxa)
    {
        var t = taxa;
        var v = valorDesejado;
        var n = prazo;

        var amortConst = v / n;
        decimal saldo = v;
        var parcelas = new List<ParcelaDto>(n);

        for (int i = 1; i <= n; i++)
        {
            var juros = saldo * t;
            var amort = amortConst;

            if (i == n)
                amort = saldo;             // garante saldo = 0

            var prest = amort + juros;
            saldo -= amort;

            parcelas.Add(new ParcelaDto
            {
                Numero            = i,
                ValorAmortizacao  = R2(amort),
                ValorJuros        = R2(juros),
                ValorPrestacao    = R2(prest)
            });
        }

        return parcelas;
    }
}
