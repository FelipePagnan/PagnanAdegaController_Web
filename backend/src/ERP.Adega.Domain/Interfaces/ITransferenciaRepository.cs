using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;

namespace ERP.Adega.Domain.Interfaces;

public interface ITransferenciaRepository : IRepositoryBase<Transferencia>
{
    Task<Transferencia?> ObterComDetalhesAsync(Guid id, CancellationToken ct = default);
    Task<int> ProximoNumeroAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Transferencia>> ListarAsync(Guid? filialId, StatusTransferencia? status,
        int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<int> ContarAsync(Guid? filialId, StatusTransferencia? status, CancellationToken ct = default);
}
