using ERP.Adega.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Adega.Infrastructure.Persistence.Configurations;

public class CaixaConfiguration : IEntityTypeConfiguration<Caixa>
{
    public void Configure(EntityTypeBuilder<Caixa> builder)
    {
        builder.ToTable("caixas");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.SaldoAbertura).HasColumnType("decimal(18,2)");
        builder.Property(c => c.TotalEntradas).HasColumnType("decimal(18,2)");
        builder.Property(c => c.TotalSaidas).HasColumnType("decimal(18,2)");
        builder.Property(c => c.SaldoFechamento).HasColumnType("decimal(18,2)");
        builder.Property(c => c.ObservacaoFechamento).HasMaxLength(500);
        builder.HasOne(c => c.Filial).WithMany().HasForeignKey(c => c.FilialId);
        builder.HasOne(c => c.Usuario).WithMany().HasForeignKey(c => c.UsuarioId);
        builder.Ignore(c => c.SaldoAtual);
        builder.HasIndex(c => new { c.FilialId, c.Status });
    }
}

public class ContaPagarConfiguration : IEntityTypeConfiguration<ContaPagar>
{
    public void Configure(EntityTypeBuilder<ContaPagar> builder)
    {
        builder.ToTable("contas_pagar");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Descricao).HasMaxLength(300).IsRequired();
        builder.Property(c => c.ValorOriginal).HasColumnType("decimal(18,2)");
        builder.Property(c => c.ValorPago).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Observacoes).HasMaxLength(500);
        builder.HasOne(c => c.Filial).WithMany().HasForeignKey(c => c.FilialId);
        builder.HasOne(c => c.Fornecedor).WithMany().HasForeignKey(c => c.FornecedorId);
        builder.HasIndex(c => new { c.FilialId, c.Status, c.DataVencimento });
    }
}

public class ContaReceberConfiguration : IEntityTypeConfiguration<ContaReceber>
{
    public void Configure(EntityTypeBuilder<ContaReceber> builder)
    {
        builder.ToTable("contas_receber");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Descricao).HasMaxLength(300).IsRequired();
        builder.Property(c => c.ValorOriginal).HasColumnType("decimal(18,2)");
        builder.Property(c => c.ValorRecebido).HasColumnType("decimal(18,2)");
        builder.Property(c => c.TaxaOperadora).HasColumnType("decimal(5,2)");
        builder.Property(c => c.ValorLiquido).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Observacoes).HasMaxLength(500);
        builder.HasOne(c => c.Filial).WithMany().HasForeignKey(c => c.FilialId);
        builder.HasOne(c => c.Cliente).WithMany().HasForeignKey(c => c.ClienteId);
        builder.HasIndex(c => new { c.FilialId, c.Status, c.DataVencimento });
    }
}
