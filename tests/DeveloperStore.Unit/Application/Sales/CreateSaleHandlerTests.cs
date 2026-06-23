using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Application.Sales.CreateSale;
using DeveloperStore.Domain.Events;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.Domain.Repositories;
using DeveloperStore.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DeveloperStore.Unit.Application.Sales;

public class CreateSaleHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPersistSaleAndPublishDomainEvents()
    {
        // Arrange
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        var handler = new CreateSaleHandler(repository, publisher, TimeProvider.System);

        var command = new CreateSaleCommand(
            SaleNumber.Create("SALE-100"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [CreateInput("product-1", "Product 1", 4, 10)]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.SaleNumber.Should().Be("SALE-100");
        await repository.Received(1).AddAsync(Arg.Any<DeveloperStore.Domain.Entities.Sale>(), Arg.Any<CancellationToken>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await publisher.Received(1).PublishAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRejectDuplicatedSaleNumber()
    {
        // Arrange
        var repository = Substitute.For<ISaleRepository>();
        var publisher = Substitute.For<IDomainEventPublisher>();
        repository.ExistsByNumberAsync(SaleNumber.Create("SALE-100"), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateSaleHandler(repository, publisher, TimeProvider.System);
        var command = new CreateSaleCommand(
            SaleNumber.Create("SALE-100"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "John Doe"),
            BranchReference.Create("branch-1", "Main Branch"),
            [CreateInput("product-1", "Product 1", 4, 10)]);

        // Act
        var action = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<DuplicateSaleNumberException>();
        await publisher.DidNotReceive().PublishAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    private static SaleItemInput CreateInput(string productExternalId, string productName, int quantity, decimal unitPrice)
    {
        return new SaleItemInput(
            ProductReference.Create(productExternalId, productName),
            ItemQuantity.Create(quantity),
            Money.Create(unitPrice, "item unit price", false));
    }
}
