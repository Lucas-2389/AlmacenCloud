using AlmacenCloud.Application.Abstractions;

namespace AlmacenCloud.Application.Features.Sales;

public sealed class SalesTaxCalculator(decimal taxRate) : ISalesTaxCalculator
{
    public SaleAmounts Calculate(decimal quantity, decimal unitPrice, bool taxable)
    {
        var total = decimal.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
        if (!taxable) return new SaleAmounts(total, 0, total);
        var subtotal = decimal.Round(total / (1 + taxRate), 2, MidpointRounding.AwayFromZero);
        return new SaleAmounts(subtotal, total - subtotal, total);
    }
}
