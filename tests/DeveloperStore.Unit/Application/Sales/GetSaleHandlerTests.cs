using DeveloperStore.Application.Sales.GetSale;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.Domain.Repositories;
using DeveloperStore.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Sale = DeveloperStore.Domain.Entities.Sale;
using SaleItem = DeveloperStore.Domain.Entities.SaleItem;

namespace DeveloperStore.Unit.Application.Sales;

public class GetSaleHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnSaleDto_WhenSaleExists()
    {
        var repository = Substitute.For<ISaleRepository>();
        var sale = BuildSale("SALE-G01");
        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var handler = new GetSaleHandler(repository);
        var result = await handler.Handle(new GetSaleQuery(sale.Id), CancellationToken.None);

        result.SaleNumber.Should().Be("SALE-G01");
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenSaleDoesNotExist()
    {
        var repository = Substitute.For<ISaleRepository>();
        repository.GetByIdAsync(Arg.Any<SaleId>(), Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var handler = new GetSaleHandler(repository);
        var action = () => handler.Handle(new GetSaleQuery(SaleId.New()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    private static Sale BuildSale(string saleNumber) =>
        Sale.Create(
            SaleNumber.Create(saleNumber),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [SaleItem.Create(ProductReference.Create("product-1", "Product 1"), ItemQuantity.Create(2), Money.Create(10m, "price", false))],
            DateTimeOffset.UtcNow);
}
