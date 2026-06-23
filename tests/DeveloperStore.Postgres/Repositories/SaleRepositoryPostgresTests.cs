using DeveloperStore.Domain.Entities;
using DeveloperStore.Domain.Enums;
using DeveloperStore.Domain.Repositories;
using DeveloperStore.Domain.ValueObjects;
using DeveloperStore.ORM;
using DeveloperStore.ORM.Repositories;
using FluentAssertions;
using Xunit;

namespace DeveloperStore.Postgres.Repositories;

public class SaleRepositoryPostgresTests
{
    [Fact]
    public async Task ListAsync_ShouldFilterByStatusAndCustomer_UsingPostgreSql()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var context = new DefaultContext(PostgresTestDatabase.BuildOptions());
        var repository = new SaleRepository(context);

        var activeSale = Sale.Create(
            SaleNumber.Create("SALE-PG-201"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-1", "Alice Smith"),
            BranchReference.Create("branch-1", "Downtown"),
            [CreateItem("product-1", "Product 1", 4, 10m)],
            DateTimeOffset.UtcNow);

        var cancelledSale = Sale.Create(
            SaleNumber.Create("SALE-PG-202"),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create("customer-2", "Bob Stone"),
            BranchReference.Create("branch-2", "Airport"),
            [CreateItem("product-2", "Product 2", 4, 10m)],
            DateTimeOffset.UtcNow);
        cancelledSale.Cancel(DateTimeOffset.UtcNow);

        await repository.AddAsync(activeSale, CancellationToken.None);
        await repository.AddAsync(cancelledSale, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var filter = new SaleListFilter(
            null,
            TextFilter.Create("Alice*"),
            null,
            SaleStatus.NotCancelled,
            null,
            new PageRequest(1, 10, "soldAt asc, saleNumber asc"));

        var result = await repository.ListAsync(filter, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items.Single().SaleNumber.Should().Be("SALE-PG-201");
    }

    [Theory]
    [InlineData("SALE-PG-EXACT-001", "SALE-PG-EXACT-001", 1)]
    [InlineData("SALE-PG-PREFIX-*", "SALE-PG-PREFIX-001", 2)]
    [InlineData("*SALE-PG-SUFFIX", "001-SALE-PG-SUFFIX", 2)]
    [InlineData("*PG-CONTAINS-*", "SALE-PG-CONTAINS-001", 2)]
    public async Task ListAsync_ShouldFilterBySaleNumber_UsingPostgreSql(string filterValue, string expectedSaleNumber, int expectedCount)
    {
        await PostgresTestDatabase.ResetAsync();
        await using var context = new DefaultContext(PostgresTestDatabase.BuildOptions());
        var repository = new SaleRepository(context);

        await repository.AddAsync(CreateSale("SALE-PG-EXACT-001", "Customer A", "Branch A"), CancellationToken.None);
        await repository.AddAsync(CreateSale("SALE-PG-PREFIX-001", "Customer B", "Branch B"), CancellationToken.None);
        await repository.AddAsync(CreateSale("SALE-PG-PREFIX-002", "Customer C", "Branch C"), CancellationToken.None);
        await repository.AddAsync(CreateSale("001-SALE-PG-SUFFIX", "Customer D", "Branch D"), CancellationToken.None);
        await repository.AddAsync(CreateSale("002-SALE-PG-SUFFIX", "Customer E", "Branch E"), CancellationToken.None);
        await repository.AddAsync(CreateSale("SALE-PG-CONTAINS-001", "Customer F", "Branch F"), CancellationToken.None);
        await repository.AddAsync(CreateSale("SALE-PG-CONTAINS-002", "Customer G", "Branch G"), CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var filter = new SaleListFilter(
            SaleNumberFilter.Create(filterValue),
            null,
            null,
            null,
            null,
            new PageRequest(1, 10));

        var result = await repository.ListAsync(filter, CancellationToken.None);

        result.Items.Should().HaveCount(expectedCount);
        result.Items.Should().Contain(s => s.SaleNumber == expectedSaleNumber);
    }

    [Fact]
    public async Task ListAsync_ShouldPaginateBySaleNumber_UsingPostgreSql()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var context = new DefaultContext(PostgresTestDatabase.BuildOptions());
        var repository = new SaleRepository(context);

        for (var i = 1; i <= 5; i++)
        {
            await repository.AddAsync(CreateSale($"SALE-PG-PAGE-{i:D3}", $"Customer {i}", $"Branch {i}"), CancellationToken.None);
        }

        await repository.SaveChangesAsync(CancellationToken.None);

        var filter = new SaleListFilter(
            SaleNumberFilter.Create("SALE-PG-PAGE-*"),
            null,
            null,
            null,
            null,
            new PageRequest(1, 2, "saleNumber asc"));

        var result = await repository.ListAsync(filter, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.Items.ElementAt(0).SaleNumber.Should().Be("SALE-PG-PAGE-001");
        result.Items.ElementAt(1).SaleNumber.Should().Be("SALE-PG-PAGE-002");
    }

    private static Sale CreateSale(string saleNumber, string customerName, string branchName)
    {
        return Sale.Create(
            SaleNumber.Create(saleNumber),
            SoldAt.Create(DateTimeOffset.UtcNow),
            CustomerReference.Create($"customer-{saleNumber}", customerName),
            BranchReference.Create($"branch-{saleNumber}", branchName),
            [CreateItem("product-1", "Product 1", 1, 100m)],
            DateTimeOffset.UtcNow);
    }

    private static SaleItem CreateItem(string productExternalId, string productName, int quantity, decimal unitPrice)
    {
        return SaleItem.Create(
            ProductReference.Create(productExternalId, productName),
            ItemQuantity.Create(quantity),
            Money.Create(unitPrice, "item unit price", false));
    }
}
