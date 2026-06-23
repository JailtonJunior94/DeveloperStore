using DeveloperStore.Domain.Common;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.Domain.ValueObjects;

namespace DeveloperStore.Domain.Entities;

public sealed class SaleItem : BaseEntity<SaleItemId>
{
    private SaleItem()
    {
    }

    private SaleItem(ProductReference product, ItemQuantity quantity, Money unitPrice)
    {
        Id = SaleItemId.New();
        Product = product;
        Quantity = quantity;
        UnitPrice = unitPrice;
        RecalculateTotals();
    }

    public SaleId SaleId { get; private set; }

    public ProductReference Product { get; private set; } = null!;

    public ItemQuantity Quantity { get; private set; }

    public Money UnitPrice { get; private set; }

    public DiscountRate DiscountPercentage { get; private set; }

    public Money DiscountAmount { get; private set; } = Money.Zero;

    public Money TotalAmount { get; private set; } = Money.Zero;

    public bool IsCancelled { get; private set; }

    public static SaleItem Create(ProductReference product, ItemQuantity quantity, Money unitPrice)
    {
        return new SaleItem(product, quantity, unitPrice);
    }

    internal void AttachToSale(SaleId saleId)
    {
        SaleId = saleId;
    }

    public void Cancel()
    {
        if (IsCancelled)
        {
            return;
        }

        IsCancelled = true;
        DiscountPercentage = DiscountRate.Zero;
        DiscountAmount = Money.Zero;
        TotalAmount = Money.Zero;
    }

    private void RecalculateTotals()
    {
        DiscountPercentage = Quantity.Value switch
        {
            >= 10 and <= 20 => DiscountRate.TwentyPercent,
            >= 4 and <= 9 => DiscountRate.TenPercent,
            _ => DiscountRate.Zero
        };

        var grossAmount = UnitPrice * Quantity;
        DiscountAmount = grossAmount * DiscountPercentage;
        TotalAmount = grossAmount - DiscountAmount;
    }
}
