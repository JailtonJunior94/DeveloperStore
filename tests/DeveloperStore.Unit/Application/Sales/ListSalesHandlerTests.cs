using DeveloperStore.Application.Sales.ListSales;
using DeveloperStore.Domain.Enums;
using DeveloperStore.Domain.Repositories;
using DeveloperStore.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Sale = DeveloperStore.Domain.Entities.Sale;
using SaleItem = DeveloperStore.Domain.Entities.SaleItem;

namespace DeveloperStore.Unit.Application.Sales;

public class ListSalesHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnPagedResponse_WhenSalesExist()
    {
        var repository = Substitute.For<ISaleRepository>();
        var sale = BuildSale("SALE-L01");
        repository.ListAsync(Arg.Any<SaleListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Sale>([sale], 1, 10, 1));

        var handler = new ListSalesHandler(repository);
        var result = await handler.Handle(
            new ListSalesQuery(null, null, null, null, null, null, null),
            CancellationToken.None);

        result.Items.Should().ContainSingle(s => s.SaleNumber == "SALE-L01");
        result.TotalCount.Should().Be(1);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPage_WhenNoSalesMatch()
    {
        var repository = Substitute.For<ISaleRepository>();
        repository.ListAsync(Arg.Any<SaleListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Sale>([], 1, 10, 0));

        var handler = new ListSalesHandler(repository);
        var result = await handler.Handle(
            new ListSalesQuery(null, null, null, null, null, null, null),
            CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldForwardAllFiltersToRepository()
    {
        var repository = Substitute.For<ISaleRepository>();
        repository.ListAsync(Arg.Any<SaleListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Sale>([], 2, 5, 0));

        var handler = new ListSalesHandler(repository);
        var minSoldAt = DateTimeOffset.UtcNow.AddDays(-7);
        var maxSoldAt = DateTimeOffset.UtcNow;

        await handler.Handle(
            new ListSalesQuery("SALE-*", "John", "Main", SaleStatus.NotCancelled, minSoldAt, maxSoldAt, "soldAt desc", 2, 5),
            CancellationToken.None);

        await repository.Received(1).ListAsync(
            Arg.Is<SaleListFilter>(f =>
                f.SaleNumber == "SALE-*" &&
                f.CustomerName == "John" &&
                f.BranchName == "Main" &&
                f.Status == SaleStatus.NotCancelled &&
                f.MinSoldAt == minSoldAt &&
                f.MaxSoldAt == maxSoldAt &&
                f.Order == "soldAt desc" &&
                f.PageNumber == 2 &&
                f.PageSize == 5),
            Arg.Any<CancellationToken>());
    }

    private static Sale BuildSale(string saleNumber) =>
        Sale.Create(
            SaleNumber.Create(saleNumber),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [SaleItem.Create(ProductReference.Create("product-1", "Product 1"), ItemQuantity.Create(4), Money.Create(10m, "item unit price", false))],
            DateTimeOffset.UtcNow);
}
