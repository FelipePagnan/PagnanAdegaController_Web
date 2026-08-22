using System.Text.Json;
using ERP.Adega.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Adega.Infrastructure.Persistence.Configurations;

public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("empresas");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RazaoSocial).HasMaxLength(200).IsRequired();
        builder.Property(e => e.NomeFantasia).HasMaxLength(200);
        builder.Property(e => e.CNPJ).HasMaxLength(18).IsRequired();
        builder.HasIndex(e => e.CNPJ).IsUnique();
        builder.Property(e => e.InscricaoEstadual).HasMaxLength(20);
        builder.Property(e => e.Telefone).HasMaxLength(20);
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.OwnsOne(e => e.Endereco, end =>
        {
            end.Property(a => a.Logradouro).HasColumnName("endereco_logradouro").HasMaxLength(200);
            end.Property(a => a.Numero).HasColumnName("endereco_numero").HasMaxLength(20);
            end.Property(a => a.Complemento).HasColumnName("endereco_complemento").HasMaxLength(100);
            end.Property(a => a.Bairro).HasColumnName("endereco_bairro").HasMaxLength(100);
            end.Property(a => a.Cidade).HasColumnName("endereco_cidade").HasMaxLength(100);
            end.Property(a => a.UF).HasColumnName("endereco_uf").HasMaxLength(2);
            end.Property(a => a.CEP).HasColumnName("endereco_cep").HasMaxLength(10);
        });
        builder.HasMany(e => e.Filiais).WithOne(f => f.Empresa).HasForeignKey(f => f.EmpresaId);
    }
}

public class FilialConfiguration : IEntityTypeConfiguration<Filial>
{
    public void Configure(EntityTypeBuilder<Filial> builder)
    {
        builder.ToTable("filiais");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Nome).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Codigo).HasMaxLength(20).IsRequired();
        builder.HasIndex(f => new { f.EmpresaId, f.Codigo }).IsUnique();
        builder.OwnsOne(f => f.Endereco, end =>
        {
            end.Property(a => a.Logradouro).HasColumnName("endereco_logradouro").HasMaxLength(200);
            end.Property(a => a.Numero).HasColumnName("endereco_numero").HasMaxLength(20);
            end.Property(a => a.Complemento).HasColumnName("endereco_complemento").HasMaxLength(100);
            end.Property(a => a.Bairro).HasColumnName("endereco_bairro").HasMaxLength(100);
            end.Property(a => a.Cidade).HasColumnName("endereco_cidade").HasMaxLength(100);
            end.Property(a => a.UF).HasColumnName("endereco_uf").HasMaxLength(2);
            end.Property(a => a.CEP).HasColumnName("endereco_cep").HasMaxLength(10);
        });
    }
}

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("categorias");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nome).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Descricao).HasMaxLength(500);
        builder.HasOne(c => c.CategoriaPai).WithMany(c => c.Subcategorias).HasForeignKey(c => c.CategoriaPaiId);
    }
}

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("produtos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nome).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Descricao).HasMaxLength(1000);
        builder.Property(p => p.PrecoVenda).HasColumnType("decimal(18,2)");
        builder.Property(p => p.PrecoCusto).HasColumnType("decimal(18,2)");
        builder.HasOne(p => p.Categoria).WithMany().HasForeignKey(p => p.CategoriaId);
        builder.HasMany(p => p.CodigosBarras).WithOne(c => c.Produto).HasForeignKey(c => c.ProdutoId);
        builder.HasMany(p => p.Embalagens).WithOne(e => e.Produto).HasForeignKey(e => e.ProdutoId);
        builder.HasIndex(p => p.Nome);
        builder.HasIndex(p => p.Ativo);
    }
}

public class CodigoBarrasConfiguration : IEntityTypeConfiguration<CodigoBarras>
{
    public void Configure(EntityTypeBuilder<CodigoBarras> builder)
    {
        builder.ToTable("codigos_barras");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Codigo).HasMaxLength(50).IsRequired();
        builder.HasIndex(c => c.Codigo).IsUnique();
    }
}

public class EmbalagemConfiguration : IEntityTypeConfiguration<Embalagem>
{
    public void Configure(EntityTypeBuilder<Embalagem> builder)
    {
        builder.ToTable("embalagens");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Nome).HasMaxLength(100).IsRequired();
        builder.Property(e => e.CodigoBarras).HasMaxLength(50);
        builder.Property(e => e.PrecoSugerido).HasColumnType("decimal(18,2)");
    }
}

public class LoteConfiguration : IEntityTypeConfiguration<Lote>
{
    public void Configure(EntityTypeBuilder<Lote> builder)
    {
        builder.ToTable("lotes");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Codigo).HasMaxLength(50).IsRequired();
        builder.Property(l => l.CustoUnitario).HasColumnType("decimal(18,4)");
        builder.Property(l => l.NotaFiscal).HasMaxLength(100);
        builder.HasOne(l => l.Produto).WithMany().HasForeignKey(l => l.ProdutoId);
        builder.HasOne(l => l.Filial).WithMany().HasForeignKey(l => l.FilialId);
        builder.HasOne(l => l.Fornecedor).WithMany().HasForeignKey(l => l.FornecedorId);
        builder.HasIndex(l => new { l.ProdutoId, l.FilialId, l.DataValidade });
    }
}

public class EstoqueProdutoConfiguration : IEntityTypeConfiguration<EstoqueProduto>
{
    public void Configure(EntityTypeBuilder<EstoqueProduto> builder)
    {
        builder.ToTable("estoque_produtos");
        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.Produto).WithMany().HasForeignKey(e => e.ProdutoId);
        builder.HasOne(e => e.Filial).WithMany().HasForeignKey(e => e.FilialId);
        builder.HasIndex(e => new { e.ProdutoId, e.FilialId }).IsUnique();
        builder.Property(e => e.LocalizacaoFisica).HasMaxLength(100);
        builder.Ignore(e => e.EstoqueDisponivel); // Calculado
    }
}

public class MovimentacaoEstoqueConfiguration : IEntityTypeConfiguration<MovimentacaoEstoque>
{
    public void Configure(EntityTypeBuilder<MovimentacaoEstoque> builder)
    {
        builder.ToTable("movimentacoes_estoque");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Motivo).HasMaxLength(500);
        builder.Property(m => m.DocumentoOrigem).HasMaxLength(100);
        builder.HasOne(m => m.Produto).WithMany().HasForeignKey(m => m.ProdutoId);
        builder.HasOne(m => m.Filial).WithMany().HasForeignKey(m => m.FilialId);
        builder.HasOne(m => m.Lote).WithMany().HasForeignKey(m => m.LoteId);
        builder.HasOne(m => m.Usuario).WithMany().HasForeignKey(m => m.UsuarioId);
        builder.HasIndex(m => new { m.ProdutoId, m.FilialId, m.CriadoEm });
        builder.HasIndex(m => m.Tipo);
    }
}

public class FornecedorConfiguration : IEntityTypeConfiguration<Fornecedor>
{
    public void Configure(EntityTypeBuilder<Fornecedor> builder)
    {
        builder.ToTable("fornecedores");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.RazaoSocial).HasMaxLength(200).IsRequired();
        builder.Property(f => f.NomeFantasia).HasMaxLength(200);
        builder.Property(f => f.CNPJ).HasMaxLength(18).IsRequired();
        builder.HasIndex(f => f.CNPJ).IsUnique();
        builder.Property(f => f.Observacoes).HasMaxLength(1000);
        builder.OwnsOne(f => f.Contato, c =>
        {
            c.Property(a => a.Telefone).HasColumnName("contato_telefone").HasMaxLength(20);
            c.Property(a => a.Celular).HasColumnName("contato_celular").HasMaxLength(20);
            c.Property(a => a.Email).HasColumnName("contato_email").HasMaxLength(200);
            c.Property(a => a.NomeContato).HasColumnName("contato_nome").HasMaxLength(100);
        });
        builder.OwnsOne(f => f.Endereco, end =>
        {
            end.Property(a => a.Logradouro).HasColumnName("endereco_logradouro").HasMaxLength(200);
            end.Property(a => a.Numero).HasColumnName("endereco_numero").HasMaxLength(20);
            end.Property(a => a.Complemento).HasColumnName("endereco_complemento").HasMaxLength(100);
            end.Property(a => a.Bairro).HasColumnName("endereco_bairro").HasMaxLength(100);
            end.Property(a => a.Cidade).HasColumnName("endereco_cidade").HasMaxLength(100);
            end.Property(a => a.UF).HasColumnName("endereco_uf").HasMaxLength(2);
            end.Property(a => a.CEP).HasColumnName("endereco_cep").HasMaxLength(10);
        });
    }
}

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nome).HasMaxLength(200).IsRequired();
        builder.Property(c => c.CPF).HasMaxLength(14);
        builder.Property(c => c.CNPJ).HasMaxLength(18);
        builder.Property(c => c.Observacoes).HasMaxLength(1000);
        builder.HasIndex(c => c.CPF).IsUnique().HasFilter("\"CPF\" IS NOT NULL");
        builder.OwnsOne(c => c.Contato, ct =>
        {
            ct.Property(a => a.Telefone).HasColumnName("contato_telefone").HasMaxLength(20);
            ct.Property(a => a.Celular).HasColumnName("contato_celular").HasMaxLength(20);
            ct.Property(a => a.Email).HasColumnName("contato_email").HasMaxLength(200);
            ct.Property(a => a.NomeContato).HasColumnName("contato_nome").HasMaxLength(100);
        });
        builder.OwnsOne(c => c.Endereco, end =>
        {
            end.Property(a => a.Logradouro).HasColumnName("endereco_logradouro").HasMaxLength(200);
            end.Property(a => a.Numero).HasColumnName("endereco_numero").HasMaxLength(20);
            end.Property(a => a.Complemento).HasColumnName("endereco_complemento").HasMaxLength(100);
            end.Property(a => a.Bairro).HasColumnName("endereco_bairro").HasMaxLength(100);
            end.Property(a => a.Cidade).HasColumnName("endereco_cidade").HasMaxLength(100);
            end.Property(a => a.UF).HasColumnName("endereco_uf").HasMaxLength(2);
            end.Property(a => a.CEP).HasColumnName("endereco_cep").HasMaxLength(10);
        });
    }
}

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Nome).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.SenhaHash).HasMaxLength(200).IsRequired();
        builder.HasOne(u => u.Perfil).WithMany().HasForeignKey(u => u.PerfilId);
        builder.HasOne(u => u.Empresa).WithMany().HasForeignKey(u => u.EmpresaId);
        builder.HasMany(u => u.FiliaisPermitidas).WithOne(uf => uf.Usuario).HasForeignKey(uf => uf.UsuarioId);
    }
}

public class UsuarioFilialConfiguration : IEntityTypeConfiguration<UsuarioFilial>
{
    public void Configure(EntityTypeBuilder<UsuarioFilial> builder)
    {
        builder.ToTable("usuario_filiais");
        builder.HasKey(uf => new { uf.UsuarioId, uf.FilialId });
        builder.HasOne(uf => uf.Filial).WithMany().HasForeignKey(uf => uf.FilialId);
    }
}

public class PerfilConfiguration : IEntityTypeConfiguration<Perfil>
{
    public void Configure(EntityTypeBuilder<Perfil> builder)
    {
        builder.ToTable("perfis");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nome).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Descricao).HasMaxLength(500);
        builder.HasOne(p => p.Empresa).WithMany().HasForeignKey(p => p.EmpresaId);

        // Permissões como JSON array com ValueConverter para SQLite
        builder.Property<List<string>>("_permissoes")
            .HasColumnName("permissoes")
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );
    }
}

public class AuditoriaConfiguration : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        builder.ToTable("auditorias");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Entidade).HasMaxLength(100).IsRequired();
        builder.Property(a => a.ValorAnterior).HasColumnType("TEXT");
        builder.Property(a => a.ValorPosterior).HasColumnType("TEXT");
        builder.Property(a => a.Motivo).HasMaxLength(500);
        builder.Property(a => a.IP).HasMaxLength(50);
        builder.HasOne(a => a.Usuario).WithMany().HasForeignKey(a => a.UsuarioId);
        builder.HasIndex(a => new { a.EmpresaId, a.Entidade, a.CriadoEm });
        builder.HasIndex(a => a.EntidadeId);
    }
}
