using DeveloperStore.Domain.Entities;
using DeveloperStore.Domain.Enums;
using DeveloperStore.Domain.Repositories;
using DeveloperStore.Domain.ValueObjects;
using DeveloperStore.ORM;
using DeveloperStore.ORM.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DeveloperStore.Integration.Repositories;

public class SaleRepositoryTests
{
    [Fact]
    public async Task ListAsync_ShouldFilterByStatusAndCustomer()
    {
        await using var context = BuildContext();
        var repository = new SaleRepository(context);

        var activeSale = Sale.Create(
            SaleNumber.Create("SALE-201"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "Alice Smith"),
            BranchReference.Create("branch-1", "Downtown"),
            [CreateItem("product-1", "Product 1", 4, 10)],
            DateTimeOffset.UtcNow);

        var cancelledSale = Sale.Create(
            SaleNumber.Create("SALE-202"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-2", "Bob Stone"),
            BranchReference.Create("branch-2", "Airport"),
            [CreateItem("product-2", "Product 2", 4, 10)],
            DateTimeOffset.UtcNow);
        cancelledSale.Cancel(DateTimeOffset.UtcNow);

        await repository.AddAsync(activeSale, CancellationToken.None);
        await repository.AddAsync(cancelledSale, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var result = await repository.ListAsync(
            new SaleListFilter(null, "Alice*", null, SaleStatus.NotCancelled, null, null, null, 1, 10),
            CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items.Single().SaleNumber.Value.Should().Be("SALE-201");
    }

    private static DefaultContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DefaultContext(options);
    }

    private static SaleItem CreateItem(string productExternalId, string productName, int quantity, decimal unitPrice)
    {
        return SaleItem.Create(
            ProductReference.Create(productExternalId, productName),
            ItemQuantity.Create(quantity),
            Money.Create(unitPrice, "item unit price", false));
    }
}
