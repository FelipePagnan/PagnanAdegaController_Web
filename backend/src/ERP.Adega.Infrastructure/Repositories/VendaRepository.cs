using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Interfaces;
using ERP.Adega.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.Adega.Infrastructure.Repositories;

public class VendaRepository : RepositoryBase<Venda>, IVendaRepository
{
    public VendaRepository(AdegaDbContext context) : base(context) { }

    public async Task<Venda?> ObterComDetalhesAsync(Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(v => v.Itens)
            .Include(v => v.Pagamentos)
            .Include(v => v.Usuario)
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<int> ProximoNumeroAsync(Guid filialId, CancellationToken ct = default)
    {
        var ultimo = await _dbSet
            .Where(v => v.FilialId == filialId)
            .OrderByDescending(v => v.Numero)
            .Select(v => v.Numero)
            .FirstOrDefaultAsync(ct);
        return ultimo + 1;
    }

    public async Task<IReadOnlyList<Venda>> ListarAsync(
        Guid filialId, DateTime? inicio, DateTime? fim,
        int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(v => v.Itens)
            .Include(v => v.Pagamentos)
            .Include(v => v.Usuario)
            .Where(v => v.FilialId == filialId);

        if (inicio.HasValue) query = query.Where(v => v.CriadoEm >= inicio.Value);
        if (fim.HasValue) query = query.Where(v => v.CriadoEm <= fim.Value);

        return await query
            .OrderByDescending(v => v.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);
    }

    public async Task<int> ContarAsync(Guid filialId, DateTime? inicio, DateTime? fim, CancellationToken ct = default)
    {
        var query = _dbSet.Where(v => v.FilialId == filialId);
        if (inicio.HasValue) query = query.Where(v => v.CriadoEm >= inicio.Value);
        if (fim.HasValue) query = query.Where(v => v.CriadoEm <= fim.Value);
        return await query.CountAsync(ct);
    }
}

public class DevolucaoRepository : IDevolucaoRepository
{
    private readonly AdegaDbContext _context;
    public DevolucaoRepository(AdegaDbContext context) => _context = context;

    public async Task AdicionarAsync(Devolucao devolucao, CancellationToken ct = default)
        => await _context.Set<Devolucao>().AddAsync(devolucao, ct);
}
