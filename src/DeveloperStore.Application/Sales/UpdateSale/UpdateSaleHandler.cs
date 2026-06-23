using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.Entities;
using DeveloperStore.Domain.Events;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.Domain.Repositories;
using MediatR;

namespace DeveloperStore.Application.Sales.UpdateSale;

public sealed class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, SaleDto>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IDomainEventPublisher _domainEventPublisher;
    private readonly TimeProvider _timeProvider;

    public UpdateSaleHandler(
        ISaleRepository saleRepository,
        IDomainEventPublisher domainEventPublisher,
        TimeProvider timeProvider)
    {
        _saleRepository = saleRepository;
        _domainEventPublisher = domainEventPublisher;
        _timeProvider = timeProvider;
    }

    public async Task<SaleDto> Handle(UpdateSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"sale '{request.Id}' was not found");

        var existingSaleByNumber = await _saleRepository.GetByNumberAsync(request.SaleNumber, cancellationToken);
        if (existingSaleByNumber is not null && existingSaleByNumber.Id != request.Id)
        {
            throw new ConflictException($"sale number '{request.SaleNumber}' already exists");
        }

        sale.Update(
            request.SaleNumber,
            request.SoldAt,
            request.Customer,
            request.Branch,
            request.Items.Select(item => SaleItem.Create(item.Product, item.Quantity, item.UnitPrice)),
            _timeProvider.GetUtcNow());

        var domainEvents = sale.DequeueDomainEvents();
        await _saleRepository.SaveChangesAsync(cancellationToken);
        await _domainEventPublisher.PublishAsync(domainEvents, cancellationToken);

        return sale.ToDto();
    }
}
