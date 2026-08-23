using ERP.Adega.Domain.Enums;

namespace ERP.Adega.Application.DTOs;

// === Caixa ===
public record CaixaDto(
    Guid Id, int Numero, Guid FilialId, string UsuarioNome, StatusCaixa Status,
    decimal SaldoAbertura, decimal TotalEntradas, decimal TotalSaidas,
    decimal SaldoAtual, decimal SaldoFechamento,
    string? ObservacaoFechamento, DateTime CriadoEm, DateTime? FechadoEm
);

public record AbrirCaixaRequest(Guid FilialId, decimal SaldoAbertura);
public record FecharCaixaRequest(string? Observacao);

// === Contas a Pagar ===
public record ContaPagarDto(
    Guid Id, Guid FilialId, string? FornecedorNome, string Descricao,
    decimal ValorOriginal, decimal? ValorPago, DateTime DataVencimento,
    DateTime? DataPagamento, StatusConta Status, int? Parcela, int? TotalParcelas,
    string? Observacoes, bool Vencida, DateTime CriadoEm
);

public record CriarContaPagarRequest(
    Guid FilialId, string Descricao, decimal Valor, DateTime DataVencimento,
    Guid? FornecedorId = null, string? Observacoes = null
);

public record PagarContaRequest(decimal ValorPago);

// === Contas a Receber ===
public record ContaReceberDto(
    Guid Id, Guid FilialId, string? ClienteNome, string Descricao,
    decimal ValorOriginal, decimal? ValorRecebido, DateTime DataVencimento,
    DateTime? DataRecebimento, StatusConta Status, string? FormaPagamento,
    decimal? TaxaOperadora, decimal? ValorLiquido, string? Observacoes, DateTime CriadoEm
);

public record CriarContaReceberRequest(
    Guid FilialId, string Descricao, decimal Valor, DateTime DataVencimento,
    Guid? ClienteId = null, string? Observacoes = null
);

// === Fluxo de Caixa ===
public record FluxoCaixaDto(
    decimal TotalPagar, decimal TotalReceber, decimal Saldo,
    int ContasPagarAbertas, int ContasReceberAbertas,
    int ContasVencidas,
    IReadOnlyList<FluxoItemDto> Itens
);

public record FluxoItemDto(
    string Tipo, string Descricao, decimal Valor, DateTime Data, StatusConta Status
);
