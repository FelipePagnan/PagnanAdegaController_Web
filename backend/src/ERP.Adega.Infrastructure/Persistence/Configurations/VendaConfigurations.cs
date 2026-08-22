using ERP.Adega.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Adega.Infrastructure.Persistence.Configurations;

public class VendaConfiguration : IEntityTypeConfiguration<Venda>
{
    public void Configure(EntityTypeBuilder<Venda> builder)
    {
        builder.ToTable("vendas");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Desconto).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Total).HasColumnType("decimal(18,2)");
        builder.Property(v => v.MotivoCancelamento).HasMaxLength(500);
        builder.HasOne(v => v.Filial).WithMany().HasForeignKey(v => v.FilialId);
        builder.HasOne(v => v.Usuario).WithMany().HasForeignKey(v => v.UsuarioId);
        builder.HasOne(v => v.Cliente).WithMany().HasForeignKey(v => v.ClienteId);
        builder.HasMany(v => v.Itens).WithOne().HasForeignKey(i => i.VendaId);
        builder.HasMany(v => v.Pagamentos).WithOne().HasForeignKey(p => p.VendaId);
        builder.HasIndex(v => new { v.FilialId, v.Numero }).IsUnique();
        builder.HasIndex(v => v.CriadoEm);
    }
}

public class ItemVendaConfiguration : IEntityTypeConfiguration<ItemVenda>
{
    public void Configure(EntityTypeBuilder<ItemVenda> builder)
    {
        builder.ToTable("itens_venda");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ProdutoNome).HasMaxLength(200);
        builder.Property(i => i.PrecoUnitario).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Total).HasColumnType("decimal(18,2)");
        builder.Property(i => i.EmbalagemNome).HasMaxLength(100);
        builder.Ignore(i => i.QuantidadeUnidadeBase);
    }
}

public class PagamentoVendaConfiguration : IEntityTypeConfiguration<PagamentoVenda>
{
    public void Configure(EntityTypeBuilder<PagamentoVenda> builder)
    {
        builder.ToTable("pagamentos_venda");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Valor).HasColumnType("decimal(18,2)");
        builder.Property(p => p.TaxaValor).HasColumnType("decimal(18,4)");
        builder.Property(p => p.ValorLiquido).HasColumnType("decimal(18,2)");
    }
}

public class DevolucaoConfiguration : IEntityTypeConfiguration<Devolucao>
{
    public void Configure(EntityTypeBuilder<Devolucao> builder)
    {
        builder.ToTable("devolucoes");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Motivo).HasMaxLength(500).IsRequired();
        builder.Property(d => d.ValorDevolvido).HasColumnType("decimal(18,2)");
        builder.HasMany(d => d.Itens).WithOne().HasForeignKey(i => i.DevolucaoId);
    }
}

public class ItemDevolucaoConfiguration : IEntityTypeConfiguration<ItemDevolucao>
{
    public void Configure(EntityTypeBuilder<ItemDevolucao> builder)
    {
        builder.ToTable("itens_devolucao");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.PrecoUnitario).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Total).HasColumnType("decimal(18,2)");
    }
}
