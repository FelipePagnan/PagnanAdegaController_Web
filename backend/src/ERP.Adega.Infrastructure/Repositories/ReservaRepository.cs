using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using ERP.Adega.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.Adega.Infrastructure.Repositories;

public class ReservaRepository : RepositoryBase<Reserva>, IReservaRepository
{
    public ReservaRepository(AdegaDbContext context) : base(context) { }

    public async Task<Reserva?> ObterComDetalhesAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.Include(r => r.Itens).Include(r => r.Cliente)
            .Include(r => r.Usuario).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<int> ProximoNumeroAsync(Guid filialId, CancellationToken ct = default)
    {
        var ultimo = await _dbSet.Where(r => r.FilialId == filialId)
            .OrderByDescending(r => r.Numero).Select(r => r.Numero).FirstOrDefaultAsync(ct);
        return ultimo + 1;
    }

    public async Task<IReadOnlyList<Reserva>> ListarAsync(Guid filialId, StatusReserva? status,
        int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var query = _dbSet.Include(r => r.Itens).Include(r => r.Cliente).Where(r => r.FilialId == filialId);
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        return await query.OrderByDescending(r => r.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina).ToListAsync(ct);
    }

    public async Task<int> ContarAsync(Guid filialId, StatusReserva? status, CancellationToken ct = default)
    {
        var query = _dbSet.Where(r => r.FilialId == filialId);
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        return await query.CountAsync(ct);
    }
}
