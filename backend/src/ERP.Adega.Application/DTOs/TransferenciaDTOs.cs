using ERP.Adega.Domain.Enums;

namespace ERP.Adega.Application.DTOs;

public record TransferenciaDto(
    Guid Id, int Numero, string FilialOrigemNome, string FilialDestinoNome,
    StatusTransferencia Status, int TotalItens, string SolicitanteNome,
    string? Observacoes, DateTime? AprovadoEm, DateTime? EnviadoEm,
    DateTime? RecebidoEm, string? MotivoCancelamento, DateTime CriadoEm,
    IReadOnlyList<ItemTransferenciaDto> Itens
);

public record TransferenciaResumoDto(
    Guid Id, int Numero, string FilialOrigemNome, string FilialDestinoNome,
    StatusTransferencia Status, int TotalItens, string SolicitanteNome, DateTime CriadoEm
);

public record ItemTransferenciaDto(Guid Id, Guid ProdutoId, string ProdutoNome, int Quantidade, int QuantidadeRecebida);

public record CriarTransferenciaRequest(
    Guid FilialOrigemId, Guid FilialDestinoId, string? Observacoes,
    List<ItemTransferenciaRequest> Itens
);

public record ItemTransferenciaRequest(Guid ProdutoId, string ProdutoNome, int Quantidade);
public record CancelarTransferenciaRequest(string Motivo);
