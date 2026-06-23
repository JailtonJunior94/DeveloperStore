using DeveloperStore.Domain.Entities;
using DeveloperStore.Domain.Enums;
using DeveloperStore.Domain.Events;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.Domain.ValueObjects;
using FluentAssertions;
using Xunit;
#pragma warning disable CA1062

namespace DeveloperStore.Unit.Domain.Sales;

public class SaleTests
{
    [Theory]
    [InlineData(1, 10, 0, 10)]
    [InlineData(3, 10, 0, 30)]
    [InlineData(4, 10, 4, 36)]
    [InlineData(9, 10, 9, 81)]
    [InlineData(10, 10, 20, 80)]
    [InlineData(20, 10, 40, 160)]
    public void Create_ShouldApplyDiscountTiers(int quantity, decimal unitPrice, decimal expectedDiscountAmount, decimal expectedTotal)
    {
        // Arrange
        var sale = Sale.Create(
            SaleNumber.Create("SALE-001"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [CreateItem("product-1", "Product 1", quantity, unitPrice)],
            DateTimeOffset.UtcNow);

        // Act
        var item = sale.Items.Single();

        // Assert
        sale.TotalAmount.Value.Should().Be(expectedTotal);
        item.DiscountAmount.Value.Should().Be(expectedDiscountAmount);
    }

    [Fact]
    public void Create_ShouldRejectMoreThanTwentyUnitsPerProduct()
    {
        // Arrange
        var action = () => CreateItem("product-1", "Product 1", 21, 10);

        // Act / Assert
        action.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("cannot sell more than 20 units of the same product");
    }

    [Fact]
    public void CancelItem_ShouldRecalculateSaleTotal()
    {
        // Arrange
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

        // Act
        sale.CancelItem(itemToCancel.Id, DateTimeOffset.UtcNow);

        // Assert
        sale.TotalAmount.Value.Should().Be(10);
        sale.Items.First().IsCancelled.Should().BeTrue();
        sale.Status.Should().Be(SaleStatus.NotCancelled);
    }

    [Fact]
    public void Cancel_ShouldMarkSaleAsCancelledAndZeroTotals()
    {
        // Arrange
        var sale = Sale.Create(
            SaleNumber.Create("SALE-003"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [CreateItem("product-1", "Product 1", 10, 10)],
            DateTimeOffset.UtcNow);

        // Act
        sale.Cancel(DateTimeOffset.UtcNow);

        // Assert
        sale.Status.Should().Be(SaleStatus.Cancelled);
        sale.TotalAmount.Value.Should().Be(0);
        sale.Items.Should().OnlyContain(item => item.IsCancelled);
    }

    [Fact]
    public void CancelItem_ShouldNotPublishAdditionalEvent_WhenItemIsAlreadyCancelled()
    {
        // Arrange — two items so cancelling one does not auto-cancel the sale
        var sale = Sale.Create(
            SaleNumber.Create("SALE-004"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [CreateItem("product-1", "Product 1", 4, 10), CreateItem("product-2", "Product 2", 2, 5)],
            DateTimeOffset.UtcNow);
        var itemToCancel = sale.Items.First(item => item.Product.Id == "product-1");

        sale.DequeueDomainEvents();

        // Act
        sale.CancelItem(itemToCancel.Id, DateTimeOffset.UtcNow);
        var firstAttemptEvents = sale.DequeueDomainEvents();

        sale.CancelItem(itemToCancel.Id, DateTimeOffset.UtcNow);
        var secondAttemptEvents = sale.DequeueDomainEvents();

        // Assert
        firstAttemptEvents.Should().ContainSingle(domainEvent => domainEvent is ItemCancelledEvent);
        secondAttemptEvents.Should().BeEmpty();
        sale.Status.Should().NotBe(SaleStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ShouldNotPublishAdditionalEvent_WhenSaleIsAlreadyCancelled()
    {
        // Arrange
        var sale = Sale.Create(
            SaleNumber.Create("SALE-005"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [CreateItem("product-1", "Product 1", 10, 10)],
            DateTimeOffset.UtcNow);

        sale.DequeueDomainEvents();

        // Act
        sale.Cancel(DateTimeOffset.UtcNow);
        var firstAttemptEvents = sale.DequeueDomainEvents();

        sale.Cancel(DateTimeOffset.UtcNow);
        var secondAttemptEvents = sale.DequeueDomainEvents();

        // Assert
        firstAttemptEvents.Should().ContainSingle(domainEvent => domainEvent is SaleCancelledEvent);
        secondAttemptEvents.Should().BeEmpty();
        sale.Status.Should().Be(SaleStatus.Cancelled);
    }

    [Fact]
    public void Create_ShouldRejectEmptyItemsList()
    {
        var action = () => Sale.Create(
            SaleNumber.Create("SALE-E01"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [],
            DateTimeOffset.UtcNow);

        action.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("a sale must contain at least one item");
    }

    [Fact]
    public void Create_ShouldRejectDuplicateProducts()
    {
        var action = () => Sale.Create(
            SaleNumber.Create("SALE-E02"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [
                CreateItem("product-1", "Product 1", 2, 10),
                CreateItem("product-1", "Product 1 Again", 3, 10)
            ],
            DateTimeOffset.UtcNow);

        action.Should().Throw<BusinessRuleValidationException>()
            .WithMessage("a sale cannot contain duplicate products");
    }

    [Fact]
    public void Create_ShouldRejectZeroQuantity()
    {
        var action = () => CreateItem("product-1", "Product 1", 0, 10);

        action.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Update_ShouldThrowConflict_WhenSaleIsCancelled()
    {
        var sale = Sale.Create(
            SaleNumber.Create("SALE-U01"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [CreateItem("product-1", "Product 1", 2, 10)],
            DateTimeOffset.UtcNow);

        sale.Cancel(DateTimeOffset.UtcNow);

        var action = () => sale.Update(
            SaleNumber.Create("SALE-U01-UPDATED"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [CreateItem("product-1", "Product 1", 2, 10)],
            DateTimeOffset.UtcNow);

        action.Should().Throw<SaleStateConflictException>()
            .WithMessage("cancelled sales cannot be changed");
    }

    [Fact]
    public void CancelItem_ShouldThrowConflict_WhenSaleIsCancelled()
    {
        var sale = Sale.Create(
            SaleNumber.Create("SALE-CI01"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [CreateItem("product-1", "Product 1", 2, 10)],
            DateTimeOffset.UtcNow);

        sale.Cancel(DateTimeOffset.UtcNow);
        var itemId = sale.Items.Single().Id;

        var action = () => sale.CancelItem(itemId, DateTimeOffset.UtcNow);

        action.Should().Throw<SaleStateConflictException>()
            .WithMessage("cancelled sales cannot be changed");
    }

    [Fact]
    public void CancelItem_ShouldCancelSale_WhenLastActiveItemCancelled()
    {
        var sale = Sale.Create(
            SaleNumber.Create("SALE-CI02"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [
                CreateItem("product-1", "Product 1", 2, 10),
                CreateItem("product-2", "Product 2", 3, 5)
            ],
            DateTimeOffset.UtcNow);

        sale.DequeueDomainEvents();

        var firstItem = sale.Items.First();
        var secondItem = sale.Items.Last();

        sale.CancelItem(firstItem.Id, DateTimeOffset.UtcNow);
        sale.Status.Should().Be(SaleStatus.NotCancelled);

        sale.CancelItem(secondItem.Id, DateTimeOffset.UtcNow);
        sale.Status.Should().Be(SaleStatus.Cancelled);
        sale.TotalAmount.Value.Should().Be(0);

        var events = sale.DequeueDomainEvents();
        events.Should().ContainSingle(e => e is SaleCancelledEvent);
    }

    [Theory]
    [InlineData(3, 10, 0)]
    [InlineData(4, 10, 0.10)]
    [InlineData(9, 10, 0.10)]
    [InlineData(10, 10, 0.20)]
    public void Create_ShouldApplyCorrectDiscountTierAtBoundaries(int quantity, decimal unitPrice, decimal expectedDiscountRate)
    {
        var sale = Sale.Create(
            SaleNumber.Create("SALE-DT01"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [CreateItem("product-1", "Product 1", quantity, unitPrice)],
            DateTimeOffset.UtcNow);

        var item = sale.Items.Single();
        item.DiscountPercentage.Value.Should().Be(expectedDiscountRate);
    }

    private static SaleItem CreateItem(string productExternalId, string productName, int quantity, decimal unitPrice)
    {
        return SaleItem.Create(
            ProductReference.Create(productExternalId, productName),
            ItemQuantity.Create(quantity),
            Money.Create(unitPrice, "item unit price", false));
    }
}
