using DeveloperStore.Domain.Entities;
using DeveloperStore.Domain.Enums;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DeveloperStore.Unit.Domain.Sales;

public class SaleTests
{
    [Theory]
    [InlineData(1, 10, 0, 10)]
    [InlineData(4, 10, 4, 36)]
    [InlineData(10, 10, 20, 80)]
    public void Create_ShouldApplyDiscountTiers(int quantity, decimal unitPrice, decimal expectedDiscountAmount, decimal expectedTotal)
    {
        var sale = Sale.Create(
            SaleNumber.Create("SALE-001"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [CreateItem("product-1", "Product 1", quantity, unitPrice)],
            DateTimeOffset.UtcNow);

        sale.TotalAmount.Value.Should().Be(expectedTotal);
        sale.Items.Single().DiscountAmount.Value.Should().Be(expectedDiscountAmount);
    }

    [Fact]
    public void Create_ShouldRejectMoreThanTwentyUnitsPerProduct()
    {
        var action = () => CreateItem("product-1", "Product 1", 21, 10);

        action.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("cannot sell more than 20 units of the same product");
    }

    [Fact]
    public void CancelItem_ShouldRecalculateSaleTotal()
    {
        var sale = Sale.Create(
            SaleNumber.Create("SALE-002"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [
                CreateItem("product-1", "Product 1", 4, 10),
                CreateItem("product-2", "Product 2", 2, 5)
            ],
            DateTimeOffset.UtcNow);

        var itemToCancel = sale.Items.First();
        sale.CancelItem(itemToCancel.Id, DateTimeOffset.UtcNow);

        sale.TotalAmount.Value.Should().Be(10);
        sale.Items.First().IsCancelled.Should().BeTrue();
        sale.Status.Should().Be(SaleStatus.NotCancelled);
    }

    [Fact]
    public void Cancel_ShouldMarkSaleAsCancelledAndZeroTotals()
    {
        var sale = Sale.Create(
            SaleNumber.Create("SALE-003"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [CreateItem("product-1", "Product 1", 10, 10)],
            DateTimeOffset.UtcNow);

        sale.Cancel(DateTimeOffset.UtcNow);

        sale.Status.Should().Be(SaleStatus.Cancelled);
        sale.TotalAmount.Value.Should().Be(0);
        sale.Items.Should().OnlyContain(item => item.IsCancelled);
    }

    private static SaleItem CreateItem(string productExternalId, string productName, int quantity, decimal unitPrice)
    {
        return SaleItem.Create(
            ProductReference.Create(productExternalId, productName),
            ItemQuantity.Create(quantity),
            Money.Create(unitPrice, "item unit price", false));
    }
}
