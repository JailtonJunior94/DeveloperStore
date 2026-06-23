using DeveloperStore.Application.Sales.CancelSaleItem;
using DeveloperStore.Application.Sales.Common;
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

public class CancelSaleItemHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCancelItemAndPublishEvent()
    {
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        var sale = BuildSaleWithTwoItems("SALE-CI-01");
        var itemToCancel = sale.Items.First();
        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var handler = new CancelSaleItemHandler(repository, publisher, TimeProvider.System);
        var result = await handler.Handle(new CancelSaleItemCommand(sale.Id, itemToCancel.Id), CancellationToken.None);

        result.Items.Single(i => i.Id == itemToCancel.Id.Value).IsCancelled.Should().BeTrue();
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await publisher.Received(1).PublishAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events.Any(e => e is ItemCancelledEvent)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldBeIdempotent_WhenItemAlreadyCancelled()
    {
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        var sale = BuildSaleWithTwoItems("SALE-CI-02");
        var itemToCancel = sale.Items.First();
        sale.CancelItem(itemToCancel.Id, DateTimeOffset.UtcNow);
        sale.DequeueDomainEvents();
        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var handler = new CancelSaleItemHandler(repository, publisher, TimeProvider.System);
        var result = await handler.Handle(new CancelSaleItemCommand(sale.Id, itemToCancel.Id), CancellationToken.None);

        result.Items.Single(i => i.Id == itemToCancel.Id.Value).IsCancelled.Should().BeTrue();
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

        var handler = new CancelSaleItemHandler(repository, publisher, TimeProvider.System);
        var action = () => handler.Handle(new CancelSaleItemCommand(SaleId.New(), SaleItemId.New()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenItemDoesNotExist()
    {
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        var sale = BuildSaleWithTwoItems("SALE-CI-04");
        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var handler = new CancelSaleItemHandler(repository, publisher, TimeProvider.System);
        var action = () => handler.Handle(new CancelSaleItemCommand(sale.Id, SaleItemId.New()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowConflict_WhenSaleIsCancelled()
    {
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        var sale = BuildSaleWithTwoItems("SALE-CI-05");
        var itemId = sale.Items.First().Id;
        sale.Cancel(DateTimeOffset.UtcNow);
        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var handler = new CancelSaleItemHandler(repository, publisher, TimeProvider.System);
        var action = () => handler.Handle(new CancelSaleItemCommand(sale.Id, itemId), CancellationToken.None);

        await action.Should().ThrowAsync<SaleStateConflictException>();
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Sale BuildSaleWithTwoItems(string saleNumber) =>
        Sale.Create(
            SaleNumber.Create(saleNumber),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [
                SaleItem.Create(ProductReference.Create("product-1", "Product 1"), ItemQuantity.Create(4), Money.Create(10m, "price", false)),
                SaleItem.Create(ProductReference.Create("product-2", "Product 2"), ItemQuantity.Create(2), Money.Create(5m, "price", false))
            ],
            DateTimeOffset.UtcNow);
}
