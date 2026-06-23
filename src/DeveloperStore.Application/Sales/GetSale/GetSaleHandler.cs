using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.Exceptions;
using DeveloperStore.Domain.Repositories;
using MediatR;

namespace DeveloperStore.Application.Sales.GetSale;

public sealed class GetSaleHandler : IRequestHandler<GetSaleQuery, SaleDto>
{
    private readonly ISaleRepository _saleRepository;

    public GetSaleHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<SaleDto> Handle(GetSaleQuery request, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"sale '{request.Id}' was not found");

        return sale.ToDto();
    }
}
