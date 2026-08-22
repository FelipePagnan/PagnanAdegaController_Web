using ERP.Adega.Domain.Enums;

namespace ERP.Adega.Application.DTOs;

public record PedidoCompraDto(
    Guid Id,
    int Numero,
    Guid FornecedorId,
    string FornecedorNome,
    Guid FilialId,
    StatusPedidoCompra Status,
    decimal SubTotal,
    decimal Frete,
    decimal Desconto,
    decimal Total,
    string? Observacoes,
    string? NotaFiscal,
    string UsuarioNome,
    string? AprovadoPorNome,
    DateTime? AprovadoEm,
    string? MotivoRejeicao,
    DateTime? RecebidoEm,
    DateTime CriadoEm,
    IReadOnlyList<ItemCompraDto> Itens
);

public record PedidoCompraResumoDto(
    Guid Id,
    int Numero,
    string FornecedorNome,
    StatusPedidoCompra Status,
    decimal Total,
    int TotalItens,
    string UsuarioNome,
    DateTime CriadoEm
);

public record ItemCompraDto(
    Guid Id,
    Guid ProdutoId,
    string ProdutoNome,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Total,
    int QuantidadeRecebida,
    int QuantidadeDivergente,
    string? CodigoLote,
    DateTime? DataValidade,
    string? ObservacaoRecebimento
);

// === Requests ===

public record CriarPedidoCompraRequest(
    Guid FornecedorId,
    Guid FilialId,
    decimal Frete,
    decimal Desconto,
    string? Observacoes,
    List<ItemPedidoCompraRequest> Itens
);

public record ItemPedidoCompraRequest(
    Guid ProdutoId,
    string ProdutoNome,
    int Quantidade,
    decimal PrecoUnitario,
    string? CodigoLote = null,
    DateTime? DataValidade = null
);

public record ReceberPedidoRequest(
    string? NotaFiscal,
    List<RecebimentoItemRequest> Itens
);

public record RecebimentoItemRequest(
    Guid ItemId,
    int QuantidadeRecebida,
    string? Observacao = null
);

public record RejeicaoRequest(string Motivo);
