using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;

namespace ERP.Adega.Domain.Interfaces;

public interface ICaixaRepository : IRepositoryBase<Caixa>
{
    Task<Caixa?> ObterAbertoAsync(Guid filialId, CancellationToken ct = default);
    Task<int> ProximoNumeroAsync(Guid filialId, CancellationToken ct = default);
    Task<IReadOnlyList<Caixa>> ListarAsync(Guid filialId, int pagina, int tamanhoPagina, CancellationToken ct = default);
}

public interface IContaPagarRepository : IRepositoryBase<ContaPagar>
{
    Task<IReadOnlyList<ContaPagar>> ListarAsync(Guid filialId, StatusConta? status,
        int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<int> ContarAsync(Guid filialId, StatusConta? status, CancellationToken ct = default);
    Task<decimal> TotalAbertoAsync(Guid filialId, CancellationToken ct = default);
    Task<int> ContarVencidasAsync(Guid filialId, CancellationToken ct = default);
}

public interface IContaReceberRepository : IRepositoryBase<ContaReceber>
{
    Task<IReadOnlyList<ContaReceber>> ListarAsync(Guid filialId, StatusConta? status,
        int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<int> ContarAsync(Guid filialId, StatusConta? status, CancellationToken ct = default);
    Task<decimal> TotalAbertoAsync(Guid filialId, CancellationToken ct = default);
}
