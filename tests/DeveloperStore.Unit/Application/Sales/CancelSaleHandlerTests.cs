using DeveloperStore.Application.Sales.CancelSale;
using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.Enums;
using DeveloperStore.Domain.Events;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.Domain.Repositories;
using DeveloperStore.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Sale = DeveloperStore.Domain.Entities.Sale;
using SaleItem = DeveloperStore.Domain.Entities.SaleItem;

namespace DeveloperStore.Unit.Application.Sales;

public class CancelSaleHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCancelSaleAndPublishEvent()
    {
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        var sale = BuildSale("SALE-C01");
        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var handler = new CancelSaleHandler(repository, publisher, TimeProvider.System);
        var result = await handler.Handle(new CancelSaleCommand(sale.Id), CancellationToken.None);

        result.Status.Should().Be(SaleStatus.Cancelled);
        result.TotalAmount.Should().Be(0);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await publisher.Received(1).PublishAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events.Any(e => e is SaleCancelledEvent)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldBeIdempotent_WhenSaleAlreadyCancelled()
    {
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        var sale = BuildSale("SALE-C02");
        sale.Cancel(DateTimeOffset.UtcNow);
        sale.DequeueDomainEvents();
        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var handler = new CancelSaleHandler(repository, publisher, TimeProvider.System);
        var result = await handler.Handle(new CancelSaleCommand(sale.Id), CancellationToken.None);

        result.Status.Should().Be(SaleStatus.Cancelled);
        await publisher.Received(1).PublishAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => !events.Any()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenSaleDoesNotExist()
    {
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        repository.GetByIdAsync(Arg.Any<SaleId>(), Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var handler = new CancelSaleHandler(repository, publisher, TimeProvider.System);
        var action = () => handler.Handle(new CancelSaleCommand(SaleId.New()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await publisher.DidNotReceive().PublishAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    private static Sale BuildSale(string saleNumber) =>
        Sale.Create(
            SaleNumber.Create(saleNumber),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [SaleItem.Create(ProductReference.Create("product-1", "Product 1"), ItemQuantity.Create(4), Money.Create(10m, "price", false))],
            DateTimeOffset.UtcNow);
}
