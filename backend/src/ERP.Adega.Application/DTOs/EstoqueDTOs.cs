using ERP.Adega.Domain.Enums;

namespace ERP.Adega.Application.DTOs;

public record EstoqueProdutoDto(
    Guid Id,
    Guid ProdutoId,
    string ProdutoNome,
    Guid FilialId,
    int EstoqueFisico,
    int EstoqueReservado,
    int EstoqueDisponivel,
    string? LocalizacaoFisica,
    NivelAlertaEstoque NivelAlerta,
    int? FardoQuantidade,
    int? Fardos,
    int? UnidadesRestantes,
    DateTime AtualizadoEm
);

public record MovimentacaoEstoqueDto(
    Guid Id,
    Guid ProdutoId,
    string ProdutoNome,
    TipoMovimentacao Tipo,
    int Quantidade,
    int SaldoAnterior,
    int SaldoPosterior,
    string? LoteCodigo,
    string? Motivo,
    string? DocumentoOrigem,
    string UsuarioNome,
    DateTime CriadoEm
);

public record AjusteEstoqueRequest(
    Guid ProdutoId,
    Guid FilialId,
    int NovaQuantidade,
    string Motivo
);

public record LoteDto(
    Guid Id,
    Guid ProdutoId,
    string Codigo,
    DateTime? DataFabricacao,
    DateTime? DataValidade,
    string? FornecedorNome,
    string? NotaFiscal,
    decimal CustoUnitario,
    int QuantidadeRecebida,
    int QuantidadeAtual,
    bool Vencido,
    bool Vencendo,
    DateTime CriadoEm
);
