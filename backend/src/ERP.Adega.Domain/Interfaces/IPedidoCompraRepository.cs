using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;

namespace ERP.Adega.Domain.Interfaces;

public interface IPedidoCompraRepository : IRepositoryBase<PedidoCompra>
{
    Task<PedidoCompra?> ObterComDetalhesAsync(Guid id, CancellationToken ct = default);
    Task<int> ProximoNumeroAsync(Guid filialId, CancellationToken ct = default);
    Task<IReadOnlyList<PedidoCompra>> ListarAsync(Guid filialId, StatusPedidoCompra? status,
        int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<int> ContarAsync(Guid filialId, StatusPedidoCompra? status, CancellationToken ct = default);
    Task<int> ContarPendentesAsync(Guid filialId, CancellationToken ct = default);
}
