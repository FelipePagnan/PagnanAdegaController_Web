using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ERP.Adega.Infrastructure.Persistence;

public class AdegaDbContext : DbContext, IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public AdegaDbContext(DbContextOptions<AdegaDbContext> options) : base(options) { }

    // DbSets
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Filial> Filiais => Set<Filial>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<CodigoBarras> CodigosBarras => Set<CodigoBarras>();
    public DbSet<Embalagem> Embalagens => Set<Embalagem>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<EstoqueProduto> EstoqueProdutos => Set<EstoqueProduto>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<UsuarioFilial> UsuarioFiliais => Set<UsuarioFilial>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();
    public DbSet<PagamentoVenda> PagamentosVenda => Set<PagamentoVenda>();
    public DbSet<Devolucao> Devolucoes => Set<Devolucao>();
    public DbSet<ItemDevolucao> ItensDevolucao => Set<ItemDevolucao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdegaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    // IUnitOfWork
    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
