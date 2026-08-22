using ERP.Adega.Domain.Entities;

namespace ERP.Adega.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}

public interface IRepositoryBase<T> where T : EntityBase
{
    Task<T?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ObterTodosAsync(CancellationToken ct = default);
    Task AdicionarAsync(T entidade, CancellationToken ct = default);
    void Atualizar(T entidade);
    void Remover(T entidade);
}

public interface IEmpresaRepository : IRepositoryBase<Empresa>
{
    Task<Empresa?> ObterPorCnpjAsync(string cnpj, CancellationToken ct = default);
    Task<Empresa?> ObterComFiliaisAsync(Guid id, CancellationToken ct = default);
}

public interface IFilialRepository : IRepositoryBase<Filial>
{
    Task<IReadOnlyList<Filial>> ObterPorEmpresaAsync(Guid empresaId, CancellationToken ct = default);
}

public interface IProdutoRepository : IRepositoryBase<Produto>
{
    Task<Produto?> ObterComDetalhesAsync(Guid id, CancellationToken ct = default);
    Task<Produto?> ObterPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default);
    Task<IReadOnlyList<Produto>> BuscarAsync(string? termo, Guid? categoriaId, bool? ativo,
        int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<int> ContarAsync(string? termo, Guid? categoriaId, bool? ativo, CancellationToken ct = default);
}

public interface ICategoriaRepository : IRepositoryBase<Categoria>
{
    Task<IReadOnlyList<Categoria>> ObterAtivasAsync(CancellationToken ct = default);
}

public interface IEstoqueProdutoRepository : IRepositoryBase<EstoqueProduto>
{
    Task<EstoqueProduto?> ObterAsync(Guid produtoId, Guid filialId, CancellationToken ct = default);
    Task<IReadOnlyList<EstoqueProduto>> ObterPorFilialAsync(Guid filialId, CancellationToken ct = default);
    Task<IReadOnlyList<EstoqueProduto>> ObterAlertasAsync(Guid filialId, CancellationToken ct = default);
}

public interface ILoteRepository : IRepositoryBase<Lote>
{
    /// <summary>
    /// Retorna lotes com estoque > 0, ordenados por FEFO (vencimento mais próximo primeiro).
    /// </summary>
    Task<IReadOnlyList<Lote>> ObterDisponiveisFEFOAsync(Guid produtoId, Guid filialId, CancellationToken ct = default);
    Task<IReadOnlyList<Lote>> ObterVencendoAsync(Guid filialId, int dias = 30, CancellationToken ct = default);
}

public interface IMovimentacaoEstoqueRepository
{
    Task AdicionarAsync(MovimentacaoEstoque mov, CancellationToken ct = default);
    Task AdicionarVariasAsync(IEnumerable<MovimentacaoEstoque> movs, CancellationToken ct = default);
    Task<IReadOnlyList<MovimentacaoEstoque>> ObterPorProdutoAsync(Guid produtoId, Guid filialId,
        DateTime? inicio, DateTime? fim, int pagina, int tamanhoPagina, CancellationToken ct = default);
}

public interface IFornecedorRepository : IRepositoryBase<Fornecedor>
{
    Task<Fornecedor?> ObterPorCnpjAsync(string cnpj, CancellationToken ct = default);
    Task<IReadOnlyList<Fornecedor>> ObterAtivosAsync(CancellationToken ct = default);
}

public interface IClienteRepository : IRepositoryBase<Cliente>
{
    Task<Cliente?> ObterPorCpfAsync(string cpf, CancellationToken ct = default);
}

public interface IUsuarioRepository : IRepositoryBase<Usuario>
{
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default);
    Task<Usuario?> ObterComPerfilAsync(Guid id, CancellationToken ct = default);
}

public interface IPerfilRepository : IRepositoryBase<Perfil>
{
    Task<IReadOnlyList<Perfil>> ObterPorEmpresaAsync(Guid empresaId, CancellationToken ct = default);
}

public interface IAuditoriaRepository
{
    Task AdicionarAsync(Auditoria auditoria, CancellationToken ct = default);
    Task<IReadOnlyList<Auditoria>> ConsultarAsync(Guid empresaId, string? entidade,
        DateTime? inicio, DateTime? fim, int pagina, int tamanhoPagina, CancellationToken ct = default);
}
