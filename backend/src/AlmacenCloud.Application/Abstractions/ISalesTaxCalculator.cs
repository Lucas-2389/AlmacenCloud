namespace AlmacenCloud.Application.Abstractions;

public interface ISalesTaxCalculator
{
    SaleAmounts Calculate(decimal quantity, decimal unitPrice, bool taxable);
}

public sealed record SaleAmounts(decimal Subtotal, decimal Igv, decimal Total);
