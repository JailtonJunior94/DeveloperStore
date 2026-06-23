using DeveloperStore.Domain.Entities;
using DeveloperStore.Domain.ValueObjects;

namespace DeveloperStore.Domain.Repositories;

public interface ISaleRepository
{
    Task<bool> ExistsByNumberAsync(SaleNumber saleNumber, CancellationToken cancellationToken);
    Task<Sale?> GetByIdAsync(SaleId id, CancellationToken cancellationToken);
    Task<Sale?> GetByNumberAsync(SaleNumber saleNumber, CancellationToken cancellationToken);
    Task<PagedResult<Sale>> ListAsync(SaleListFilter filter, CancellationToken cancellationToken);
    Task AddAsync(Sale sale, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
