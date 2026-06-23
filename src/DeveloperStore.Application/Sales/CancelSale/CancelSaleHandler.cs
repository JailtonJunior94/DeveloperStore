using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.Events;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.Domain.Repositories;
using MediatR;

namespace DeveloperStore.Application.Sales.CancelSale;

public sealed class CancelSaleHandler : IRequestHandler<CancelSaleCommand, SaleDto>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IDomainEventPublisher _domainEventPublisher;
    private readonly TimeProvider _timeProvider;

    public CancelSaleHandler(
        ISaleRepository saleRepository,
        IDomainEventPublisher domainEventPublisher,
        TimeProvider timeProvider)
    {
        _saleRepository = saleRepository;
        _domainEventPublisher = domainEventPublisher;
        _timeProvider = timeProvider;
    }

    public async Task<SaleDto> Handle(CancelSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"sale '{request.Id}' was not found");

        sale.Cancel(_timeProvider.GetUtcNow());

        await _saleRepository.SaveChangesAsync(cancellationToken);
        var domainEvents = sale.DequeueDomainEvents();
        await _domainEventPublisher.PublishAsync(domainEvents, cancellationToken);

        return sale.ToDto();
    }
}
