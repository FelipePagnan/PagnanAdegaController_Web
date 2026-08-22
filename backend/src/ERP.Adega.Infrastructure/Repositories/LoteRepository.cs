using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Interfaces;
using ERP.Adega.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.Adega.Infrastructure.Repositories;

public class LoteRepository : RepositoryBase<Lote>, ILoteRepository
{
    public LoteRepository(AdegaDbContext context) : base(context) { }

    /// <summary>
    /// RN-009: FEFO — retorna lotes com estoque, ordenados por vencimento mais próximo.
    /// Lotes sem validade ficam por último (não têm prioridade).
    /// Lotes vencidos são excluídos da saída.
    /// </summary>
    public async Task<IReadOnlyList<Lote>> ObterDisponiveisFEFOAsync(
        Guid produtoId, Guid filialId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(l => l.ProdutoId == produtoId
                     && l.FilialId == filialId
                     && l.QuantidadeAtual > 0
                     && (!l.DataValidade.HasValue || l.DataValidade.Value.Date >= DateTime.UtcNow.Date))
            .OrderBy(l => l.DataValidade.HasValue ? 0 : 1) // Com validade primeiro
            .ThenBy(l => l.DataValidade)                     // Vencimento mais próximo
            .ThenBy(l => l.CriadoEm)                        // FIFO como desempate
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Lote>> ObterVencendoAsync(
        Guid filialId, int dias = 30, CancellationToken ct = default)
    {
        var limite = DateTime.UtcNow.Date.AddDays(dias);

        return await _dbSet
            .Include(l => l.Produto)
            .Where(l => l.FilialId == filialId
                     && l.QuantidadeAtual > 0
                     && l.DataValidade.HasValue
                     && l.DataValidade.Value.Date <= limite)
            .OrderBy(l => l.DataValidade)
            .ToListAsync(ct);
    }
}
