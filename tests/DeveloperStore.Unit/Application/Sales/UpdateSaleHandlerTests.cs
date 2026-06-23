using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Application.Sales.UpdateSale;
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

public class UpdateSaleHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUpdateSaleAndPublishEvent()
    {
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        var sale = BuildSale("SALE-U01");
        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        repository.GetByNumberAsync(SaleNumber.Create("SALE-U01-NEW"), Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var handler = new UpdateSaleHandler(repository, publisher, TimeProvider.System);
        var command = new UpdateSaleCommand(
            sale.Id,
            SaleNumber.Create("SALE-U01-NEW"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-updated", "Updated Customer"),
            BranchReference.Create("branch-updated", "Updated Branch"),
            [new SaleItemInput(ProductReference.Create("product-1", "Product 1"), ItemQuantity.Create(10), Money.Create(10m, "price", false))]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.SaleNumber.Should().Be("SALE-U01-NEW");
        result.CustomerName.Should().Be("Updated Customer");
        result.TotalAmount.Should().Be(80m);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await publisher.Received(1).PublishAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events.Any(e => e is SaleModifiedEvent)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenSaleDoesNotExist()
    {
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        repository.GetByIdAsync(Arg.Any<SaleId>(), Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var handler = new UpdateSaleHandler(repository, publisher, TimeProvider.System);
        var action = () => handler.Handle(BuildUpdateCommand(SaleId.New(), "SALE-GHOST"), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await publisher.DidNotReceive().PublishAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowConflict_WhenUpdatingCancelledSale()
    {
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        var sale = BuildSale("SALE-U02");
        sale.Cancel(DateTimeOffset.UtcNow);
        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        repository.GetByNumberAsync(Arg.Any<SaleNumber>(), Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var handler = new UpdateSaleHandler(repository, publisher, TimeProvider.System);
        var action = () => handler.Handle(BuildUpdateCommand(sale.Id, "SALE-U02-UPDATED"), CancellationToken.None);

        await action.Should().ThrowAsync<SaleStateConflictException>();
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowDuplicateSaleNumber_WhenNumberBelongsToAnotherSale()
    {
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        var sale = BuildSale("SALE-U03");
        var other = BuildSale("SALE-U03-CONFLICT");
        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        repository.GetByNumberAsync(SaleNumber.Create("SALE-U03-CONFLICT"), Arg.Any<CancellationToken>()).Returns(other);

        var handler = new UpdateSaleHandler(repository, publisher, TimeProvider.System);
        var action = () => handler.Handle(BuildUpdateCommand(sale.Id, "SALE-U03-CONFLICT"), CancellationToken.None);

        await action.Should().ThrowAsync<DuplicateSaleNumberException>();
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenSaleNumberRemainsTheSame()
    {
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        var sale = BuildSale("SALE-U04");
        repository.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);
        repository.GetByNumberAsync(SaleNumber.Create("SALE-U04"), Arg.Any<CancellationToken>()).Returns(sale);

        var handler = new UpdateSaleHandler(repository, publisher, TimeProvider.System);
        var result = await handler.Handle(BuildUpdateCommand(sale.Id, "SALE-U04"), CancellationToken.None);

        result.SaleNumber.Should().Be("SALE-U04");
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Sale BuildSale(string saleNumber) =>
        Sale.Create(
            SaleNumber.Create(saleNumber),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [SaleItem.Create(ProductReference.Create("product-1", "Product 1"), ItemQuantity.Create(4), Money.Create(10m, "price", false))],
            DateTimeOffset.UtcNow);

    private static UpdateSaleCommand BuildUpdateCommand(SaleId id, string saleNumber) =>
        new(id,
            SaleNumber.Create(saleNumber),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [new SaleItemInput(ProductReference.Create("product-1", "Product 1"), ItemQuantity.Create(4), Money.Create(10m, "price", false))]);
}
