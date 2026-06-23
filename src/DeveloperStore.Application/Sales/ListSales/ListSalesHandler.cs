using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.Repositories;
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
        var result = await _saleRepository.ListAsync(
            new SaleListFilter(
                request.SaleNumber,
                request.Customer,
                request.Branch,
                request.Status,
                request.MinSoldAt,
                request.MaxSoldAt,
                request.Order,
                request.PageNumber,
                request.PageSize),
            cancellationToken);

        return result.ToResponse();
    }
}
