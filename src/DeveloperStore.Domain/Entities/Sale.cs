using DeveloperStore.Domain.Common;
using DeveloperStore.Domain.Enums;
using DeveloperStore.Domain.Events;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.Domain.ValueObjects;

namespace DeveloperStore.Domain.Entities;

public sealed class Sale : BaseEntity<SaleId>
{
    private readonly List<SaleItem> _items = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    private Sale()
    {
    }

    private Sale(
        SaleNumber saleNumber,
        SoldAt soldAt,
        CustomerReference customer,
        BranchReference branch)
    {
        Id = SaleId.New();
        SaleNumber = saleNumber;
        SoldAt = soldAt;
        Customer = customer;
        Branch = branch;
        Status = SaleStatus.NotCancelled;
    }

    public SaleNumber SaleNumber { get; private set; }

    public SoldAt SoldAt { get; private set; }

    public CustomerReference Customer { get; private set; } = null!;

    public BranchReference Branch { get; private set; } = null!;

    public Money TotalAmount { get; private set; } = Money.Zero;

    public SaleStatus Status { get; private set; }

    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

    public static Sale Create(
        SaleNumber saleNumber,
        SoldAt soldAt,
        CustomerReference customer,
        BranchReference branch,
        IEnumerable<SaleItem> items,
        DateTimeOffset occurredOn)
    {
        var sale = new Sale(
            saleNumber,
            soldAt,
            customer,
            branch);

        sale.ReplaceItems(items);
        sale.RecordEvent(new SaleCreatedEvent(sale.Id, sale.SaleNumber, occurredOn));
        return sale;
    }

    public void Update(
        SaleNumber saleNumber,
        SoldAt soldAt,
        CustomerReference customer,
        BranchReference branch,
        IEnumerable<SaleItem> items,
        DateTimeOffset occurredOn)
    {
        EnsureNotCancelled();

        SaleNumber = saleNumber;
        SoldAt = soldAt;
        Customer = customer;
        Branch = branch;

        ReplaceItems(items);
        RecordEvent(new SaleModifiedEvent(Id, SaleNumber, occurredOn));
    }

    public void Cancel(DateTimeOffset occurredOn)
    {
        if (Status == SaleStatus.Cancelled)
        {
            return;
        }

        Status = SaleStatus.Cancelled;

        foreach (var item in _items)
        {
            item.Cancel();
        }

        RecalculateTotalAmount();
        RecordEvent(new SaleCancelledEvent(Id, SaleNumber, occurredOn));
    }

    public void CancelItem(SaleItemId itemId, DateTimeOffset occurredOn)
    {
        EnsureNotCancelled();

        var item = _items.SingleOrDefault(current => current.Id == itemId)
            ?? throw new NotFoundException($"sale item '{itemId}' was not found");

        if (item.IsCancelled)
        {
            return;
        }

        item.Cancel();
        RecalculateTotalAmount();
        RecordEvent(new ItemCancelledEvent(Id, itemId, SaleNumber, occurredOn));

        if (_items.All(current => current.IsCancelled))
        {
            Status = SaleStatus.Cancelled;
            RecordEvent(new SaleCancelledEvent(Id, SaleNumber, occurredOn));
        }
    }

    public IReadOnlyCollection<IDomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }

    private void ReplaceItems(IEnumerable<SaleItem> items)
    {
        var materializedItems = items?.ToArray() ?? [];
        if (materializedItems.Length == 0)
        {
            throw new BusinessRuleValidationException("a sale must contain at least one item");
        }

        var duplicateProductIds = materializedItems
            .GroupBy(item => item.Product.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateProductIds.Length > 0)
        {
            throw new BusinessRuleValidationException("a sale cannot contain duplicate products");
        }

        _items.Clear();
        foreach (var item in materializedItems)
        {
            item.AttachToSale(Id);
            _items.Add(item);
        }

        RecalculateTotalAmount();
    }

    private void RecalculateTotalAmount()
    {
        TotalAmount = _items
            .Where(item => !item.IsCancelled)
            .Select(item => item.TotalAmount)
            .Aggregate(Money.Zero, static (total, current) => total + current);
    }

    private void EnsureNotCancelled()
    {
        if (Status == SaleStatus.Cancelled)
        {
            throw new SaleStateConflictException("cancelled sales cannot be changed");
        }
    }

    private void RecordEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
