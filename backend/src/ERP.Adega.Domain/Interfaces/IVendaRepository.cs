using ERP.Adega.Domain.Entities;

namespace ERP.Adega.Domain.Interfaces;

public interface IVendaRepository : IRepositoryBase<Venda>
{
    Task<Venda?> ObterComDetalhesAsync(Guid id, CancellationToken ct = default);
    Task<int> ProximoNumeroAsync(Guid filialId, CancellationToken ct = default);
    Task<IReadOnlyList<Venda>> ListarAsync(Guid filialId, DateTime? inicio, DateTime? fim,
        int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<int> ContarAsync(Guid filialId, DateTime? inicio, DateTime? fim, CancellationToken ct = default);
}

public interface IDevolucaoRepository
{
    Task AdicionarAsync(Devolucao devolucao, CancellationToken ct = default);
}
