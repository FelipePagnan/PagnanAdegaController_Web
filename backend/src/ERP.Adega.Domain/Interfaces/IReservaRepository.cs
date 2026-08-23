using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;

namespace ERP.Adega.Domain.Interfaces;

public interface IReservaRepository : IRepositoryBase<Reserva>
{
    Task<Reserva?> ObterComDetalhesAsync(Guid id, CancellationToken ct = default);
    Task<int> ProximoNumeroAsync(Guid filialId, CancellationToken ct = default);
    Task<IReadOnlyList<Reserva>> ListarAsync(Guid filialId, StatusReserva? status,
        int pagina, int tamanhoPagina, CancellationToken ct = default);
    Task<int> ContarAsync(Guid filialId, StatusReserva? status, CancellationToken ct = default);
}
