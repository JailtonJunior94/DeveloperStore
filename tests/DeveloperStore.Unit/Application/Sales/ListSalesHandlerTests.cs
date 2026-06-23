using DeveloperStore.Application.Sales.ListSales;
using DeveloperStore.Domain.Enums;
using DeveloperStore.Domain.Repositories;
using DeveloperStore.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DeveloperStore.Unit.Application.Sales;

public class ListSalesHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnPagedResponse_WhenSalesExist()
    {
        var repository = Substitute.For<ISaleRepository>();
        var summary = new SaleSummary(
            Guid.NewGuid(),
            "SALE-L01",
            DateTimeOffset.UtcNow,
            "John Doe",
            "Main Branch",
            36m,
            SaleStatus.NotCancelled,
            1);

        repository.ListAsync(Arg.Any<SaleListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<SaleSummary>([summary], 1, 10, 1));

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
            .Returns(new PagedResult<SaleSummary>([], 1, 10, 0));

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
            .Returns(new PagedResult<SaleSummary>([], 2, 5, 0));

        var handler = new ListSalesHandler(repository);
        var minSoldAt = DateTimeOffset.UtcNow.AddDays(-7);
        var maxSoldAt = DateTimeOffset.UtcNow;

        await handler.Handle(
            new ListSalesQuery("SALE-*", "John", "Main", SaleStatus.NotCancelled, minSoldAt, maxSoldAt, "soldAt desc", 2, 5),
            CancellationToken.None);

        await repository.Received(1).ListAsync(
            Arg.Is<SaleListFilter>(f =>
                f.SaleNumber!.Value.Text == "SALE-" &&
                f.SaleNumber.Value.Mode == StringMatchMode.StartsWith &&
                f.CustomerName!.Value.Text == "John" &&
                f.BranchName!.Value.Text == "Main" &&
                f.Status == SaleStatus.NotCancelled &&
                f.SoldAtRange!.Value.Min.Value == minSoldAt &&
                f.SoldAtRange.Value.Max.Value == maxSoldAt &&
                f.Pagination.Order == "soldAt desc" &&
                f.Pagination.PageNumber == 2 &&
                f.Pagination.PageSize == 5),
            Arg.Any<CancellationToken>());
    }
}
