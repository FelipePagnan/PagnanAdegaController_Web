using ERP.Adega.Domain.Enums;

namespace ERP.Adega.Application.DTOs;

public record VendaDto(
    Guid Id,
    int Numero,
    Guid FilialId,
    Guid? ClienteId,
    string? ClienteNome,
    StatusVenda Status,
    decimal SubTotal,
    decimal Desconto,
    decimal Total,
    decimal TotalPago,
    decimal Troco,
    int TotalItens,
    string UsuarioNome,
    string? MotivoCancelamento,
    DateTime CriadoEm,
    DateTime? FinalizadoEm,
    IReadOnlyList<ItemVendaDto> Itens,
    IReadOnlyList<PagamentoVendaDto> Pagamentos
);

public record VendaResumoDto(
    Guid Id,
    int Numero,
    StatusVenda Status,
    decimal Total,
    int TotalItens,
    string UsuarioNome,
    string FormaPagamentoPrincipal,
    DateTime CriadoEm
);

public record ItemVendaDto(
    Guid Id,
    Guid ProdutoId,
    string ProdutoNome,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Total,
    string? EmbalagemNome,
    int? UnidadesPorEmbalagem
);

public record PagamentoVendaDto(
    Guid Id,
    FormaPagamento Forma,
    decimal Valor,
    decimal TaxaPercentual,
    decimal TaxaValor,
    decimal ValorLiquido
);

// === Requests ===

public record CriarVendaRequest(
    Guid FilialId,
    Guid? ClienteId,
    List<ItemVendaRequest> Itens,
    List<PagamentoVendaRequest> Pagamentos,
    decimal Desconto = 0
);

public record ItemVendaRequest(
    Guid ProdutoId,
    int Quantidade,
    decimal PrecoUnitario,
    Guid? EmbalagemId = null
);

public record PagamentoVendaRequest(
    FormaPagamento Forma,
    decimal Valor
);

public record CancelarVendaRequest(string Motivo);

public record DevolucaoRequest(
    Guid VendaId,
    string Motivo,
    List<ItemDevolucaoRequest> Itens
);

public record ItemDevolucaoRequest(
    Guid ProdutoId,
    int Quantidade,
    decimal PrecoUnitario
);
