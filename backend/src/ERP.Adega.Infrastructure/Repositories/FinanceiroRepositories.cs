using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using ERP.Adega.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.Adega.Infrastructure.Repositories;

public class CaixaRepository : RepositoryBase<Caixa>, ICaixaRepository
{
    public CaixaRepository(AdegaDbContext context) : base(context) { }

    public async Task<Caixa?> ObterAbertoAsync(Guid filialId, CancellationToken ct = default)
        => await _dbSet.Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.FilialId == filialId && c.Status == StatusCaixa.Aberto, ct);

    public async Task<int> ProximoNumeroAsync(Guid filialId, CancellationToken ct = default)
    {
        var ultimo = await _dbSet.Where(c => c.FilialId == filialId)
            .OrderByDescending(c => c.Numero).Select(c => c.Numero).FirstOrDefaultAsync(ct);
        return ultimo + 1;
    }

    public async Task<IReadOnlyList<Caixa>> ListarAsync(Guid filialId, int pagina, int tamanhoPagina, CancellationToken ct = default)
        => await _dbSet.Include(c => c.Usuario)
            .Where(c => c.FilialId == filialId)
            .OrderByDescending(c => c.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina).ToListAsync(ct);
}

public class ContaPagarRepository : RepositoryBase<ContaPagar>, IContaPagarRepository
{
    public ContaPagarRepository(AdegaDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ContaPagar>> ListarAsync(Guid filialId, StatusConta? status,
        int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var query = _dbSet.Include(c => c.Fornecedor).Where(c => c.FilialId == filialId);
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);
        return await query.OrderBy(c => c.DataVencimento)
            .Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina).ToListAsync(ct);
    }

    public async Task<int> ContarAsync(Guid filialId, StatusConta? status, CancellationToken ct = default)
    {
        var query = _dbSet.Where(c => c.FilialId == filialId);
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);
        return await query.CountAsync(ct);
    }

    public async Task<decimal> TotalAbertoAsync(Guid filialId, CancellationToken ct = default)
        => await _dbSet.Where(c => c.FilialId == filialId && c.Status == StatusConta.Aberta)
            .SumAsync(c => c.ValorOriginal, ct);

    public async Task<int> ContarVencidasAsync(Guid filialId, CancellationToken ct = default)
        => await _dbSet.CountAsync(c => c.FilialId == filialId
            && c.Status == StatusConta.Aberta && c.DataVencimento < DateTime.UtcNow, ct);
}

public class ContaReceberRepository : RepositoryBase<ContaReceber>, IContaReceberRepository
{
    public ContaReceberRepository(AdegaDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ContaReceber>> ListarAsync(Guid filialId, StatusConta? status,
        int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var query = _dbSet.Include(c => c.Cliente).Where(c => c.FilialId == filialId);
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);
        return await query.OrderBy(c => c.DataVencimento)
            .Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina).ToListAsync(ct);
    }

    public async Task<int> ContarAsync(Guid filialId, StatusConta? status, CancellationToken ct = default)
    {
        var query = _dbSet.Where(c => c.FilialId == filialId);
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);
        return await query.CountAsync(ct);
    }

    public async Task<decimal> TotalAbertoAsync(Guid filialId, CancellationToken ct = default)
        => await _dbSet.Where(c => c.FilialId == filialId && c.Status == StatusConta.Aberta)
            .SumAsync(c => c.ValorOriginal, ct);
}
