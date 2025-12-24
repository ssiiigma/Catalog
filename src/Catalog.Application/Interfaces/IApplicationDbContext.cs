using Catalog.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}