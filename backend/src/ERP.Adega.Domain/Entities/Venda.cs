using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Exceptions;

namespace ERP.Adega.Domain.Entities;

/// <summary>
/// Aggregate root de vendas.
/// RN-004: Só consome estoque disponível.
/// RN-019: Sem desconto automático por quantidade.
/// RN-020: Sem venda fiada.
/// Cancelamento ≠ Devolução.
/// </summary>
public class Venda : EntityBase
{
    public int Numero { get; private set; }
    public Guid FilialId { get; private set; }
    public Guid? ClienteId { get; private set; }
    public Guid CaixaId { get; private set; }
    public StatusVenda Status { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal Desconto { get; private set; }
    public decimal Total { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid? AutorizadoPor { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public DateTime? FinalizadoEm { get; private set; }

    // Navegação
    public Filial Filial { get; private set; } = default!;
    public Cliente? Cliente { get; private set; }
    public Usuario Usuario { get; private set; } = default!;

    private readonly List<ItemVenda> _itens = new();
    public IReadOnlyCollection<ItemVenda> Itens => _itens.AsReadOnly();

    private readonly List<PagamentoVenda> _pagamentos = new();
    public IReadOnlyCollection<PagamentoVenda> Pagamentos => _pagamentos.AsReadOnly();

    private Venda() { }

    public static Venda Criar(int numero, Guid filialId, Guid caixaId, Guid usuarioId, Guid? clienteId = null)
    {
        return new Venda
        {
            Numero = numero,
            FilialId = filialId,
            CaixaId = caixaId,
            UsuarioId = usuarioId,
            ClienteId = clienteId,
            Status = StatusVenda.Aberta,
            SubTotal = 0,
            Desconto = 0,
            Total = 0
        };
    }

    public ItemVenda AdicionarItem(Guid produtoId, string produtoNome, int quantidade,
        decimal precoUnitario, string? embalagemNome = null, int? unidadesPorEmbalagem = null)
    {
        if (Status != StatusVenda.Aberta)
            throw new DomainException("VENDA_FECHADA", "Não é possível adicionar itens a uma venda finalizada ou cancelada.");

        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser positiva.", nameof(quantidade));

        var item = new ItemVenda(Id, produtoId, produtoNome, quantidade, precoUnitario, embalagemNome, unidadesPorEmbalagem);
        _itens.Add(item);
        RecalcularTotais();
        return item;
    }

    public void AplicarDesconto(decimal desconto, Guid autorizadoPor)
    {
        if (desconto < 0)
            throw new ArgumentException("Desconto não pode ser negativo.");

        if (desconto > SubTotal)
            throw new DomainException("DESCONTO_INVALIDO", "Desconto não pode ser maior que o subtotal.");

        // RN-019: Sem desconto automático por quantidade — desconto é excepcional com autorização
        Desconto = desconto;
        AutorizadoPor = autorizadoPor;
        RecalcularTotais();
    }

    public void Finalizar()
    {
        if (Status != StatusVenda.Aberta)
            throw new DomainException("VENDA_FECHADA", "Venda já foi finalizada ou cancelada.");

        if (_itens.Count == 0)
            throw new DomainException("VENDA_VAZIA", "Não é possível finalizar uma venda sem itens.");

        var totalPago = _pagamentos.Sum(p => p.Valor);
        if (totalPago < Total)
            throw new DomainException("PAGAMENTO_INSUFICIENTE",
                $"Total pago (R$ {totalPago:N2}) é menor que o total da venda (R$ {Total:N2}).");

        Status = StatusVenda.Finalizada;
        FinalizadoEm = DateTime.UtcNow;
        MarcarAtualizado();
    }

    public void Cancelar(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new OperacaoSemMotivoException("Cancelamento de venda");

        if (Status == StatusVenda.Cancelada)
            throw new DomainException("JA_CANCELADA", "Venda já está cancelada.");

        Status = StatusVenda.Cancelada;
        MotivoCancelamento = motivo.Trim();
        MarcarAtualizado();
    }

    public PagamentoVenda AdicionarPagamento(FormaPagamento forma, decimal valor, decimal taxaPercentual = 0)
    {
        if (Status != StatusVenda.Aberta)
            throw new DomainException("VENDA_FECHADA", "Não é possível adicionar pagamento a uma venda finalizada.");

        if (valor <= 0)
            throw new ArgumentException("Valor do pagamento deve ser positivo.");

        var taxa = valor * (taxaPercentual / 100);
        var liquido = valor - taxa;

        var pagamento = new PagamentoVenda(Id, forma, valor, taxaPercentual, taxa, liquido);
        _pagamentos.Add(pagamento);
        return pagamento;
    }

    public decimal TotalPago => _pagamentos.Sum(p => p.Valor);
    public decimal Troco => Math.Max(0, TotalPago - Total);
    public int TotalItens => _itens.Sum(i => i.Quantidade);

    private void RecalcularTotais()
    {
        SubTotal = _itens.Sum(i => i.Total);
        Total = SubTotal - Desconto;
        if (Total < 0) Total = 0;
    }
}

public class ItemVenda : EntityBase
{
    public Guid VendaId { get; private set; }
    public Guid ProdutoId { get; private set; }
    public string ProdutoNome { get; private set; } = default!;
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal Total { get; private set; }
    public string? EmbalagemNome { get; private set; }
    public int? UnidadesPorEmbalagem { get; private set; }

    private ItemVenda() { }

    internal ItemVenda(Guid vendaId, Guid produtoId, string produtoNome, int quantidade,
        decimal precoUnitario, string? embalagemNome, int? unidadesPorEmbalagem)
    {
        VendaId = vendaId;
        ProdutoId = produtoId;
        ProdutoNome = produtoNome;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        Total = quantidade * precoUnitario;
        EmbalagemNome = embalagemNome;
        UnidadesPorEmbalagem = unidadesPorEmbalagem;
    }

    /// <summary>
    /// Quantidade real em unidade base (para baixar estoque).
    /// Se vendeu 2 fardos de 12, são 24 unidades base.
    /// </summary>
    public int QuantidadeUnidadeBase =>
        UnidadesPorEmbalagem.HasValue ? Quantidade * UnidadesPorEmbalagem.Value : Quantidade;
}

public class PagamentoVenda : EntityBase
{
    public Guid VendaId { get; private set; }
    public FormaPagamento Forma { get; private set; }
    public decimal Valor { get; private set; }
    public decimal TaxaPercentual { get; private set; }
    public decimal TaxaValor { get; private set; }
    public decimal ValorLiquido { get; private set; }

    private PagamentoVenda() { }

    internal PagamentoVenda(Guid vendaId, FormaPagamento forma, decimal valor,
        decimal taxaPercentual, decimal taxaValor, decimal valorLiquido)
    {
        VendaId = vendaId;
        Forma = forma;
        Valor = valor;
        TaxaPercentual = taxaPercentual;
        TaxaValor = taxaValor;
        ValorLiquido = valorLiquido;
    }
}

/// <summary>
/// Devolução — processo separado do cancelamento.
/// Pode ser parcial (devolver apenas alguns itens).
/// </summary>
public class Devolucao : EntityBase
{
    public Guid VendaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Motivo { get; private set; } = default!;
    public decimal ValorDevolvido { get; private set; }

    private readonly List<ItemDevolucao> _itens = new();
    public IReadOnlyCollection<ItemDevolucao> Itens => _itens.AsReadOnly();

    private Devolucao() { }

    public static Devolucao Criar(Guid vendaId, Guid usuarioId, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new OperacaoSemMotivoException("Devolução");

        return new Devolucao
        {
            VendaId = vendaId,
            UsuarioId = usuarioId,
            Motivo = motivo.Trim(),
            ValorDevolvido = 0
        };
    }

    public void AdicionarItem(Guid produtoId, int quantidade, decimal precoUnitario)
    {
        _itens.Add(new ItemDevolucao(Id, produtoId, quantidade, precoUnitario));
        ValorDevolvido = _itens.Sum(i => i.Total);
    }
}

public class ItemDevolucao : EntityBase
{
    public Guid DevolucaoId { get; private set; }
    public Guid ProdutoId { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal Total { get; private set; }

    private ItemDevolucao() { }

    internal ItemDevolucao(Guid devolucaoId, Guid produtoId, int quantidade, decimal precoUnitario)
    {
        DevolucaoId = devolucaoId;
        ProdutoId = produtoId;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        Total = quantidade * precoUnitario;
    }
}
