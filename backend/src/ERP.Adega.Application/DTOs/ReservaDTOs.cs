using ERP.Adega.Domain.Enums;

namespace ERP.Adega.Application.DTOs;

public record ReservaDto(
    Guid Id, int Numero, string ClienteNome, StatusReserva Status,
    decimal ValorTotal, decimal ValorAdiantamento, decimal ValorRestante,
    DateTime DataLimite, string? Observacoes, string UsuarioNome,
    DateTime? RetiradoEm, string? MotivoCancelamento, bool Expirada,
    DateTime CriadoEm, IReadOnlyList<ItemReservaDto> Itens
);

public record ReservaResumoDto(
    Guid Id, int Numero, string ClienteNome, StatusReserva Status,
    decimal ValorTotal, DateTime DataLimite, int TotalItens, bool Expirada, DateTime CriadoEm
);

public record ItemReservaDto(Guid Id, Guid ProdutoId, string ProdutoNome, int Quantidade, decimal PrecoUnitario, decimal Total);

public record CriarReservaRequest(
    Guid ClienteId, Guid FilialId, decimal ValorAdiantamento, DateTime DataLimite,
    string? Observacoes, List<ItemReservaRequest> Itens
);

public record ItemReservaRequest(Guid ProdutoId, string ProdutoNome, int Quantidade, decimal PrecoUnitario);
public record CancelarReservaRequest(string Motivo);
