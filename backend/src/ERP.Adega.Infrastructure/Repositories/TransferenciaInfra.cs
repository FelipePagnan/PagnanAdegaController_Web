using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using ERP.Adega.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Adega.Infrastructure.Repositories;

public class TransferenciaRepository : RepositoryBase<Transferencia>, ITransferenciaRepository
{
    public TransferenciaRepository(AdegaDbContext context) : base(context) { }

    public async Task<Transferencia?> ObterComDetalhesAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.Include(t => t.Itens).Include(t => t.FilialOrigem)
            .Include(t => t.FilialDestino).Include(t => t.Solicitante)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<int> ProximoNumeroAsync(CancellationToken ct = default)
    {
        var ultimo = await _dbSet.OrderByDescending(t => t.Numero)
            .Select(t => t.Numero).FirstOrDefaultAsync(ct);
        return ultimo + 1;
    }

    public async Task<IReadOnlyList<Transferencia>> ListarAsync(Guid? filialId, StatusTransferencia? status,
        int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var query = _dbSet.Include(t => t.Itens).Include(t => t.FilialOrigem)
            .Include(t => t.FilialDestino).Include(t => t.Solicitante).AsQueryable();

        if (filialId.HasValue)
            query = query.Where(t => t.FilialOrigemId == filialId.Value || t.FilialDestinoId == filialId.Value);
        if (status.HasValue) query = query.Where(t => t.Status == status.Value);

        return await query.OrderByDescending(t => t.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina).ToListAsync(ct);
    }

    public async Task<int> ContarAsync(Guid? filialId, StatusTransferencia? status, CancellationToken ct = default)
    {
        var query = _dbSet.AsQueryable();
        if (filialId.HasValue)
            query = query.Where(t => t.FilialOrigemId == filialId.Value || t.FilialDestinoId == filialId.Value);
        if (status.HasValue) query = query.Where(t => t.Status == status.Value);
        return await query.CountAsync(ct);
    }
}

// === EF Configurations ===
namespace ERP.Adega.Infrastructure.Persistence.Configurations;

public class TransferenciaConfiguration : IEntityTypeConfiguration<Transferencia>
{
    public void Configure(EntityTypeBuilder<Transferencia> builder)
    {
        builder.ToTable("transferencias");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Observacoes).HasMaxLength(500);
        builder.Property(t => t.MotivoCancelamento).HasMaxLength(500);
        builder.HasOne(t => t.FilialOrigem).WithMany().HasForeignKey(t => t.FilialOrigemId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.FilialDestino).WithMany().HasForeignKey(t => t.FilialDestinoId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.Solicitante).WithMany().HasForeignKey(t => t.SolicitanteId);
        builder.HasMany(t => t.Itens).WithOne().HasForeignKey(i => i.TransferenciaId);
        builder.HasIndex(t => t.Status);
        builder.Ignore(t => t.TotalItens);
    }
}

public class ItemTransferenciaConfiguration : IEntityTypeConfiguration<ItemTransferencia>
{
    public void Configure(EntityTypeBuilder<ItemTransferencia> builder)
    {
        builder.ToTable("itens_transferencia");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ProdutoNome).HasMaxLength(200);
    }
}
