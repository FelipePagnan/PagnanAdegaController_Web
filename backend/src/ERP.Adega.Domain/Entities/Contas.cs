using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Exceptions;

namespace ERP.Adega.Domain.Entities;

public class ContaPagar : EntityBase
{
    public Guid FilialId { get; private set; }
    public Guid? FornecedorId { get; private set; }
    public Guid? PedidoCompraId { get; private set; }
    public string Descricao { get; private set; } = default!;
    public decimal ValorOriginal { get; private set; }
    public decimal? ValorPago { get; private set; }
    public DateTime DataVencimento { get; private set; }
    public DateTime? DataPagamento { get; private set; }
    public StatusConta Status { get; private set; }
    public int? Parcela { get; private set; }
    public int? TotalParcelas { get; private set; }
    public string? Observacoes { get; private set; }
    public Guid? PagoPorId { get; private set; }

    // Navegação
    public Filial Filial { get; private set; } = default!;
    public Fornecedor? Fornecedor { get; private set; }

    private ContaPagar() { }

    public static ContaPagar Criar(Guid filialId, string descricao, decimal valor,
        DateTime dataVencimento, Guid? fornecedorId = null, Guid? pedidoCompraId = null,
        int? parcela = null, int? totalParcelas = null, string? observacoes = null)
    {
        return new ContaPagar
        {
            FilialId = filialId,
            FornecedorId = fornecedorId,
            PedidoCompraId = pedidoCompraId,
            Descricao = descricao.Trim(),
            ValorOriginal = valor,
            DataVencimento = dataVencimento,
            Status = StatusConta.Aberta,
            Parcela = parcela,
            TotalParcelas = totalParcelas,
            Observacoes = observacoes?.Trim()
        };
    }

    public void Pagar(decimal valorPago, Guid pagoPorId)
    {
        if (Status == StatusConta.Paga)
            throw new DomainException("JA_PAGA", "Conta já está paga.");

        ValorPago = valorPago;
        DataPagamento = DateTime.UtcNow;
        PagoPorId = pagoPorId;
        Status = StatusConta.Paga;
        MarcarAtualizado();
    }

    public void Cancelar()
    {
        if (Status == StatusConta.Paga)
            throw new DomainException("JA_PAGA", "Conta paga não pode ser cancelada.");
        Status = StatusConta.Cancelada;
        MarcarAtualizado();
    }

    public bool EstaVencida() => Status == StatusConta.Aberta && DataVencimento.Date < DateTime.UtcNow.Date;
}

public class ContaReceber : EntityBase
{
    public Guid FilialId { get; private set; }
    public Guid? ClienteId { get; private set; }
    public Guid? VendaId { get; private set; }
    public string Descricao { get; private set; } = default!;
    public decimal ValorOriginal { get; private set; }
    public decimal? ValorRecebido { get; private set; }
    public DateTime DataVencimento { get; private set; }
    public DateTime? DataRecebimento { get; private set; }
    public StatusConta Status { get; private set; }
    public FormaPagamento? FormaPagamento { get; private set; }
    public decimal? TaxaOperadora { get; private set; }
    public decimal? ValorLiquido { get; private set; }
    public string? Observacoes { get; private set; }

    // Navegação
    public Filial Filial { get; private set; } = default!;
    public Cliente? Cliente { get; private set; }

    private ContaReceber() { }

    public static ContaReceber Criar(Guid filialId, string descricao, decimal valor,
        DateTime dataVencimento, FormaPagamento? forma = null,
        Guid? clienteId = null, Guid? vendaId = null,
        decimal? taxaOperadora = null, string? observacoes = null)
    {
        var liquido = taxaOperadora.HasValue ? valor - (valor * taxaOperadora.Value / 100) : valor;

        return new ContaReceber
        {
            FilialId = filialId,
            ClienteId = clienteId,
            VendaId = vendaId,
            Descricao = descricao.Trim(),
            ValorOriginal = valor,
            DataVencimento = dataVencimento,
            Status = StatusConta.Aberta,
            FormaPagamento = forma,
            TaxaOperadora = taxaOperadora,
            ValorLiquido = liquido,
            Observacoes = observacoes?.Trim()
        };
    }

    public void Receber(decimal valorRecebido)
    {
        if (Status == StatusConta.Paga)
            throw new DomainException("JA_RECEBIDA", "Conta já foi recebida.");

        ValorRecebido = valorRecebido;
        DataRecebimento = DateTime.UtcNow;
        Status = StatusConta.Paga;
        MarcarAtualizado();
    }

    public void Cancelar()
    {
        if (Status == StatusConta.Paga)
            throw new DomainException("JA_RECEBIDA", "Conta recebida não pode ser cancelada.");
        Status = StatusConta.Cancelada;
        MarcarAtualizado();
    }
}
