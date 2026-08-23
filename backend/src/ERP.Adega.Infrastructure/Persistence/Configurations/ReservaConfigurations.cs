using ERP.Adega.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Adega.Infrastructure.Persistence.Configurations;

public class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.ToTable("reservas");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ValorTotal).HasColumnType("decimal(18,2)");
        builder.Property(r => r.ValorAdiantamento).HasColumnType("decimal(18,2)");
        builder.Property(r => r.ValorRestante).HasColumnType("decimal(18,2)");
        builder.Property(r => r.Observacoes).HasMaxLength(500);
        builder.Property(r => r.MotivoCancelamento).HasMaxLength(500);
        builder.HasOne(r => r.Cliente).WithMany().HasForeignKey(r => r.ClienteId);
        builder.HasOne(r => r.Filial).WithMany().HasForeignKey(r => r.FilialId);
        builder.HasOne(r => r.Usuario).WithMany().HasForeignKey(r => r.UsuarioId);
        builder.HasMany(r => r.Itens).WithOne().HasForeignKey(i => i.ReservaId);
        builder.HasIndex(r => new { r.FilialId, r.Numero }).IsUnique();
        builder.HasIndex(r => r.Status);
    }
}

public class ItemReservaConfiguration : IEntityTypeConfiguration<ItemReserva>
{
    public void Configure(EntityTypeBuilder<ItemReserva> builder)
    {
        builder.ToTable("itens_reserva");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ProdutoNome).HasMaxLength(200);
        builder.Property(i => i.PrecoUnitario).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Total).HasColumnType("decimal(18,2)");
    }
}
