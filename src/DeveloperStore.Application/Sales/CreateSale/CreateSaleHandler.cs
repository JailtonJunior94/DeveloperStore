using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.Entities;
using DeveloperStore.Domain.Events;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.Domain.Repositories;
using MediatR;

namespace DeveloperStore.Application.Sales.CreateSale;

public sealed class CreateSaleHandler : IRequestHandler<CreateSaleCommand, SaleDto>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IDomainEventPublisher _domainEventPublisher;
    private readonly TimeProvider _timeProvider;

    public CreateSaleHandler(
        ISaleRepository saleRepository,
        IDomainEventPublisher domainEventPublisher,
        TimeProvider timeProvider)
    {
        _saleRepository = saleRepository;
        _domainEventPublisher = domainEventPublisher;
        _timeProvider = timeProvider;
    }

    public async Task<SaleDto> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        if (await _saleRepository.ExistsByNumberAsync(request.SaleNumber, cancellationToken))
        {
            throw new ConflictException($"sale number '{request.SaleNumber}' already exists");
        }

        var sale = Sale.Create(
            request.SaleNumber,
            request.SoldAt,
            request.Customer,
            request.Branch,
            request.Items.Select(item => SaleItem.Create(item.Product, item.Quantity, item.UnitPrice)),
            _timeProvider.GetUtcNow());

        await _saleRepository.AddAsync(sale, cancellationToken);
        var domainEvents = sale.DequeueDomainEvents();
        await _saleRepository.SaveChangesAsync(cancellationToken);
        await _domainEventPublisher.PublishAsync(domainEvents, cancellationToken);

        return sale.ToDto();
    }
}
