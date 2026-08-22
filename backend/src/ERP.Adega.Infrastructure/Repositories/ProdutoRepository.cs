using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Interfaces;
using ERP.Adega.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.Adega.Infrastructure.Repositories;

public class ProdutoRepository : RepositoryBase<Produto>, IProdutoRepository
{
    public ProdutoRepository(AdegaDbContext context) : base(context) { }

    public async Task<Produto?> ObterComDetalhesAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(p => p.Categoria)
            .Include(p => p.CodigosBarras)
            .Include(p => p.Embalagens)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Produto?> ObterPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(p => p.Categoria)
            .Include(p => p.CodigosBarras)
            .Include(p => p.Embalagens)
            .FirstOrDefaultAsync(p => p.CodigosBarras.Any(cb => cb.Codigo == codigoBarras), ct);
    }

    public async Task<IReadOnlyList<Produto>> BuscarAsync(
        string? termo, Guid? categoriaId, bool? ativo,
        int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(p => p.Categoria)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
            query = query.Where(p => p.Nome.Contains(termo) ||
                                     p.CodigosBarras.Any(cb => cb.Codigo.Contains(termo)));

        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);

        if (ativo.HasValue)
            query = query.Where(p => p.Ativo == ativo.Value);

        return await query
            .OrderBy(p => p.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);
    }

    public async Task<int> ContarAsync(
        string? termo, Guid? categoriaId, bool? ativo, CancellationToken ct = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
            query = query.Where(p => p.Nome.Contains(termo) ||
                                     p.CodigosBarras.Any(cb => cb.Codigo.Contains(termo)));

        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);

        if (ativo.HasValue)
            query = query.Where(p => p.Ativo == ativo.Value);

        return await query.CountAsync(ct);
    }
}
