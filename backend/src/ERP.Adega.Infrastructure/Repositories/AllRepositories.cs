using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Interfaces;
using ERP.Adega.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.Adega.Infrastructure.Repositories;

public class EmpresaRepository : RepositoryBase<Empresa>, IEmpresaRepository
{
    public EmpresaRepository(AdegaDbContext context) : base(context) { }

    public async Task<Empresa?> ObterPorCnpjAsync(string cnpj, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(e => e.CNPJ == cnpj, ct);

    public async Task<Empresa?> ObterComFiliaisAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.Include(e => e.Filiais).FirstOrDefaultAsync(e => e.Id == id, ct);
}

public class CategoriaRepository : RepositoryBase<Categoria>, ICategoriaRepository
{
    public CategoriaRepository(AdegaDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Categoria>> ObterAtivasAsync(CancellationToken ct = default)
        => await _dbSet.Where(c => c.Ativa).OrderBy(c => c.Nome).ToListAsync(ct);
}

public class FilialRepository : RepositoryBase<Filial>, IFilialRepository
{
    public FilialRepository(AdegaDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Filial>> ObterPorEmpresaAsync(Guid empresaId, CancellationToken ct = default)
        => await _dbSet.Where(f => f.EmpresaId == empresaId).ToListAsync(ct);
}

public class EstoqueProdutoRepository : RepositoryBase<EstoqueProduto>, IEstoqueProdutoRepository
{
    public EstoqueProdutoRepository(AdegaDbContext context) : base(context) { }

    public async Task<EstoqueProduto?> ObterAsync(Guid produtoId, Guid filialId, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(e => e.ProdutoId == produtoId && e.FilialId == filialId, ct);

    public async Task<IReadOnlyList<EstoqueProduto>> ObterPorFilialAsync(Guid filialId, CancellationToken ct = default)
        => await _dbSet.Include(e => e.Produto).ThenInclude(p => p.Embalagens)
            .Where(e => e.FilialId == filialId).ToListAsync(ct);

    public async Task<IReadOnlyList<EstoqueProduto>> ObterAlertasAsync(Guid filialId, CancellationToken ct = default)
        => await _dbSet.Include(e => e.Produto)
            .Where(e => e.FilialId == filialId && e.Produto.EstoqueMinimo.HasValue).ToListAsync(ct);
}

public class MovimentacaoEstoqueRepository : IMovimentacaoEstoqueRepository
{
    private readonly AdegaDbContext _context;
    public MovimentacaoEstoqueRepository(AdegaDbContext context) => _context = context;

    public async Task AdicionarAsync(MovimentacaoEstoque mov, CancellationToken ct = default)
        => await _context.MovimentacoesEstoque.AddAsync(mov, ct);

    public async Task AdicionarVariasAsync(IEnumerable<MovimentacaoEstoque> movs, CancellationToken ct = default)
        => await _context.MovimentacoesEstoque.AddRangeAsync(movs, ct);

    public async Task<IReadOnlyList<MovimentacaoEstoque>> ObterPorProdutoAsync(
        Guid produtoId, Guid filialId, DateTime? inicio, DateTime? fim,
        int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var query = _context.MovimentacoesEstoque
            .Include(m => m.Usuario).Include(m => m.Lote).Include(m => m.Produto)
            .Where(m => m.ProdutoId == produtoId && m.FilialId == filialId);
        if (inicio.HasValue) query = query.Where(m => m.CriadoEm >= inicio.Value);
        if (fim.HasValue) query = query.Where(m => m.CriadoEm <= fim.Value);
        return await query.OrderByDescending(m => m.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina).ToListAsync(ct);
    }
}

public class FornecedorRepository : RepositoryBase<Fornecedor>, IFornecedorRepository
{
    public FornecedorRepository(AdegaDbContext context) : base(context) { }

    public async Task<Fornecedor?> ObterPorCnpjAsync(string cnpj, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(f => f.CNPJ == cnpj, ct);

    public async Task<IReadOnlyList<Fornecedor>> ObterAtivosAsync(CancellationToken ct = default)
        => await _dbSet.Where(f => f.Ativo).OrderBy(f => f.RazaoSocial).ToListAsync(ct);
}

public class ClienteRepository : RepositoryBase<Cliente>, IClienteRepository
{
    public ClienteRepository(AdegaDbContext context) : base(context) { }

    public async Task<Cliente?> ObterPorCpfAsync(string cpf, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(c => c.CPF == cpf, ct);
}

public class UsuarioRepository : RepositoryBase<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(AdegaDbContext context) : base(context) { }

    public async Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<Usuario?> ObterComPerfilAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.Include(u => u.Perfil).Include(u => u.Empresa)
            .Include(u => u.FiliaisPermitidas).FirstOrDefaultAsync(u => u.Id == id, ct);
}

public class PerfilRepository : RepositoryBase<Perfil>, IPerfilRepository
{
    public PerfilRepository(AdegaDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Perfil>> ObterPorEmpresaAsync(Guid empresaId, CancellationToken ct = default)
        => await _dbSet.Where(p => p.EmpresaId == empresaId).ToListAsync(ct);
}

public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly AdegaDbContext _context;
    public AuditoriaRepository(AdegaDbContext context) => _context = context;

    public async Task AdicionarAsync(Auditoria auditoria, CancellationToken ct = default)
        => await _context.Auditorias.AddAsync(auditoria, ct);

    public async Task<IReadOnlyList<Auditoria>> ConsultarAsync(
        Guid empresaId, string? entidade, DateTime? inicio, DateTime? fim,
        int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var query = _context.Auditorias.Where(a => a.EmpresaId == empresaId);
        if (!string.IsNullOrEmpty(entidade)) query = query.Where(a => a.Entidade == entidade);
        if (inicio.HasValue) query = query.Where(a => a.CriadoEm >= inicio.Value);
        if (fim.HasValue) query = query.Where(a => a.CriadoEm <= fim.Value);
        return await query.OrderByDescending(a => a.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina).ToListAsync(ct);
    }
}
