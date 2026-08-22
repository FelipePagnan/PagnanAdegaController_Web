using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using ERP.Adega.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.Adega.Infrastructure.Repositories;

public class PedidoCompraRepository : RepositoryBase<PedidoCompra>, IPedidoCompraRepository
{
    public PedidoCompraRepository(AdegaDbContext context) : base(context) { }

    public async Task<PedidoCompra?> ObterComDetalhesAsync(Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(p => p.Itens)
            .Include(p => p.Fornecedor)
            .Include(p => p.Usuario)
            .Include(p => p.Filial)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<int> ProximoNumeroAsync(Guid filialId, CancellationToken ct = default)
    {
        var ultimo = await _dbSet
            .Where(p => p.FilialId == filialId)
            .OrderByDescending(p => p.Numero)
            .Select(p => p.Numero)
            .FirstOrDefaultAsync(ct);
        return ultimo + 1;
    }

    public async Task<IReadOnlyList<PedidoCompra>> ListarAsync(
        Guid filialId, StatusPedidoCompra? status,
        int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(p => p.Itens)
            .Include(p => p.Fornecedor)
            .Include(p => p.Usuario)
            .Where(p => p.FilialId == filialId);

        if (status.HasValue) query = query.Where(p => p.Status == status.Value);

        return await query
            .OrderByDescending(p => p.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(ct);
    }

    public async Task<int> ContarAsync(Guid filialId, StatusPedidoCompra? status, CancellationToken ct = default)
    {
        var query = _dbSet.Where(p => p.FilialId == filialId);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        return await query.CountAsync(ct);
    }

    public async Task<int> ContarPendentesAsync(Guid filialId, CancellationToken ct = default)
        => await _dbSet.CountAsync(p => p.FilialId == filialId
            && p.Status == StatusPedidoCompra.AguardandoAprovacao, ct);
}
