using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.Repositories;
using DeveloperStore.Domain.ValueObjects;
using MediatR;

namespace DeveloperStore.Application.Sales.ListSales;

public sealed class ListSalesHandler : IRequestHandler<ListSalesQuery, PagedResponse<SaleSummaryDto>>
{
    private readonly ISaleRepository _saleRepository;

    public ListSalesHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<PagedResponse<SaleSummaryDto>> Handle(ListSalesQuery request, CancellationToken cancellationToken)
    {
        var filter = new SaleListFilter(
            SaleNumberFilter.Create(request.SaleNumber),
            TextFilter.Create(request.CustomerName),
            TextFilter.Create(request.BranchName),
            request.Status,
            SoldAtRange.Create(request.MinSoldAt, request.MaxSoldAt),
            new PageRequest(request.PageNumber, request.PageSize, request.Order));

        var result = await _saleRepository.ListAsync(filter, cancellationToken);

        return result.ToResponse();
    }
}
