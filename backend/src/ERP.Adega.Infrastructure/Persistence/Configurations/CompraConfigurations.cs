using ERP.Adega.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Adega.Infrastructure.Persistence.Configurations;

public class PedidoCompraConfiguration : IEntityTypeConfiguration<PedidoCompra>
{
    public void Configure(EntityTypeBuilder<PedidoCompra> builder)
    {
        builder.ToTable("pedidos_compra");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Frete).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Desconto).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Total).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Observacoes).HasMaxLength(1000);
        builder.Property(p => p.NotaFiscal).HasMaxLength(100);
        builder.Property(p => p.MotivoRejeicao).HasMaxLength(500);
        builder.HasOne(p => p.Fornecedor).WithMany().HasForeignKey(p => p.FornecedorId);
        builder.HasOne(p => p.Filial).WithMany().HasForeignKey(p => p.FilialId);
        builder.HasOne(p => p.Usuario).WithMany().HasForeignKey(p => p.UsuarioId);
        builder.HasMany(p => p.Itens).WithOne().HasForeignKey(i => i.PedidoCompraId);
        builder.HasIndex(p => new { p.FilialId, p.Numero }).IsUnique();
        builder.HasIndex(p => p.Status);
    }
}

public class ItemCompraConfiguration : IEntityTypeConfiguration<ItemCompra>
{
    public void Configure(EntityTypeBuilder<ItemCompra> builder)
    {
        builder.ToTable("itens_compra");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ProdutoNome).HasMaxLength(200);
        builder.Property(i => i.PrecoUnitario).HasColumnType("decimal(18,4)");
        builder.Property(i => i.Total).HasColumnType("decimal(18,2)");
        builder.Property(i => i.CodigoLote).HasMaxLength(50);
        builder.Property(i => i.ObservacaoRecebimento).HasMaxLength(500);
    }
}
