using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Interfaces;
using ERP.Adega.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.Adega.Infrastructure.Repositories;

public class RepositoryBase<T> : IRepositoryBase<T> where T : EntityBase
{
    protected readonly AdegaDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public RepositoryBase(AdegaDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<T>> ObterTodosAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public async Task AdicionarAsync(T entidade, CancellationToken ct = default)
        => await _dbSet.AddAsync(entidade, ct);

    public void Atualizar(T entidade)
        => _dbSet.Update(entidade);

    public void Remover(T entidade)
        => _dbSet.Remove(entidade);
}
