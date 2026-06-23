using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.Events;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.Domain.Repositories;
using MediatR;

namespace DeveloperStore.Application.Sales.CancelSaleItem;

public sealed class CancelSaleItemHandler : IRequestHandler<CancelSaleItemCommand, SaleDto>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IDomainEventPublisher _domainEventPublisher;
    private readonly TimeProvider _timeProvider;

    public CancelSaleItemHandler(
        ISaleRepository saleRepository,
        IDomainEventPublisher domainEventPublisher,
        TimeProvider timeProvider)
    {
        _saleRepository = saleRepository;
        _domainEventPublisher = domainEventPublisher;
        _timeProvider = timeProvider;
    }

    public async Task<SaleDto> Handle(CancelSaleItemCommand request, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(request.SaleId, cancellationToken)
            ?? throw new NotFoundException($"sale '{request.SaleId}' was not found");

        sale.CancelItem(request.ItemId, _timeProvider.GetUtcNow());

        await _saleRepository.SaveChangesAsync(cancellationToken);
        var domainEvents = sale.DequeueDomainEvents();
        await _domainEventPublisher.PublishAsync(domainEvents, cancellationToken);

        return sale.ToDto();
    }
}
