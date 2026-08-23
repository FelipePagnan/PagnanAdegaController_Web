using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Exceptions;

namespace ERP.Adega.Domain.Entities;

/// <summary>
/// Pedido de compra com fluxo de aprovação.
/// Aprovação configurável por valor (RN parametrizável).
/// Recebimento alimenta estoque, lote e financeiro.
/// </summary>
public class PedidoCompra : EntityBase
{
    public int Numero { get; private set; }
    public Guid FornecedorId { get; private set; }
    public Guid FilialId { get; private set; }
    public StatusPedidoCompra Status { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal Frete { get; private set; }
    public decimal Desconto { get; private set; }
    public decimal Total { get; private set; }
    public string? Observacoes { get; private set; }
    public string? NotaFiscal { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid? AprovadoPor { get; private set; }
    public DateTime? AprovadoEm { get; private set; }
    public string? MotivoRejeicao { get; private set; }
    public DateTime? RecebidoEm { get; private set; }

    // Navegação
    public Fornecedor Fornecedor { get; private set; } = default!;
    public Filial Filial { get; private set; } = default!;
    public Usuario Usuario { get; private set; } = default!;

    private readonly List<ItemCompra> _itens = new();
    public IReadOnlyCollection<ItemCompra> Itens => _itens.AsReadOnly();

    private PedidoCompra() { }

    public static PedidoCompra Criar(int numero, Guid fornecedorId, Guid filialId, Guid usuarioId,
        decimal frete = 0, decimal desconto = 0, string? observacoes = null)
    {
        return new PedidoCompra
        {
            Numero = numero,
            FornecedorId = fornecedorId,
            FilialId = filialId,
            UsuarioId = usuarioId,
            Status = StatusPedidoCompra.Rascunho,
            Frete = frete,
            Desconto = desconto,
            Observacoes = observacoes?.Trim(),
            SubTotal = 0,
            Total = 0
        };
    }

    public ItemCompra AdicionarItem(Guid produtoId, string produtoNome, int quantidade,
        decimal precoUnitario, string? codigoLote = null, DateTime? dataValidade = null)
    {
        if (Status != StatusPedidoCompra.Rascunho)
            throw new DomainException("PEDIDO_FECHADO", "Itens só podem ser adicionados em pedidos rascunho.");

        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser positiva.", nameof(quantidade));

        var item = new ItemCompra(Id, produtoId, produtoNome, quantidade, precoUnitario, codigoLote, dataValidade);
        _itens.Add(item);
        RecalcularTotais();
        return item;
    }

    /// <summary>
    /// Envia para aprovação. Determina se precisa de aprovação baseado no valor.
    /// </summary>
    public void EnviarParaAprovacao(decimal limiteAutoAprovacao)
    {
        if (Status != StatusPedidoCompra.Rascunho)
            throw new DomainException("STATUS_INVALIDO", "Apenas pedidos rascunho podem ser enviados.");

        if (_itens.Count == 0)
            throw new DomainException("PEDIDO_VAZIO", "Pedido deve ter pelo menos um item.");

        if (Total <= limiteAutoAprovacao)
        {
            // Auto-aprovação
            Status = StatusPedidoCompra.Aprovado;
            AprovadoEm = DateTime.UtcNow;
        }
        else
        {
            Status = StatusPedidoCompra.AguardandoAprovacao;
        }
        MarcarAtualizado();
    }

    public void Aprovar(Guid aprovadorId)
    {
        if (Status != StatusPedidoCompra.AguardandoAprovacao)
            throw new DomainException("STATUS_INVALIDO", "Pedido não está aguardando aprovação.");

        Status = StatusPedidoCompra.Aprovado;
        AprovadoPor = aprovadorId;
        AprovadoEm = DateTime.UtcNow;
        MarcarAtualizado();
    }

    public void Rejeitar(Guid aprovadorId, string motivo)
    {
        if (Status != StatusPedidoCompra.AguardandoAprovacao)
            throw new DomainException("STATUS_INVALIDO", "Pedido não está aguardando aprovação.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new OperacaoSemMotivoException("Rejeição de pedido de compra");

        Status = StatusPedidoCompra.Rejeitado;
        AprovadoPor = aprovadorId;
        MotivoRejeicao = motivo.Trim();
        MarcarAtualizado();
    }

    /// <summary>
    /// Registra recebimento. Cada item terá quantidade recebida preenchida.
    /// </summary>
    public void RegistrarRecebimento(string? notaFiscal)
    {
        if (Status != StatusPedidoCompra.Aprovado)
            throw new DomainException("STATUS_INVALIDO", "Apenas pedidos aprovados podem ser recebidos.");

        NotaFiscal = notaFiscal?.Trim();
        RecebidoEm = DateTime.UtcNow;

        // Verificar se todos itens foram recebidos integralmente
        var todoRecebido = _itens.All(i => i.QuantidadeRecebida == i.Quantidade);
        Status = todoRecebido ? StatusPedidoCompra.Recebido : StatusPedidoCompra.RecebidoParcial;
        MarcarAtualizado();
    }

    public void Cancelar()
    {
        if (Status == StatusPedidoCompra.Recebido || Status == StatusPedidoCompra.RecebidoParcial)
            throw new DomainException("PEDIDO_RECEBIDO", "Pedidos já recebidos não podem ser cancelados.");

        Status = StatusPedidoCompra.Cancelado;
        MarcarAtualizado();
    }

    private void RecalcularTotais()
    {
        SubTotal = _itens.Sum(i => i.Total);
        Total = SubTotal + Frete - Desconto;
        if (Total < 0) Total = 0;
    }
}

public class ItemCompra : EntityBase
{
    public Guid PedidoCompraId { get; private set; }
    public Guid ProdutoId { get; private set; }
    public string ProdutoNome { get; private set; } = default!;
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal Total { get; private set; }
    public int QuantidadeRecebida { get; private set; }
    public int QuantidadeDivergente { get; private set; }
    public string? CodigoLote { get; private set; }
    public DateTime? DataValidade { get; private set; }
    public string? ObservacaoRecebimento { get; private set; }

    private ItemCompra() { }

    internal ItemCompra(Guid pedidoId, Guid produtoId, string produtoNome,
        int quantidade, decimal precoUnitario, string? codigoLote, DateTime? dataValidade)
    {
        PedidoCompraId = pedidoId;
        ProdutoId = produtoId;
        ProdutoNome = produtoNome;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        Total = quantidade * precoUnitario;
        QuantidadeRecebida = 0;
        QuantidadeDivergente = 0;
        CodigoLote = codigoLote;
        DataValidade = dataValidade;
    }

    /// <summary>
    /// Registra conferência do recebimento.
    /// </summary>
    public void RegistrarRecebimento(int quantidadeRecebida, string? observacao = null)
    {
        if (quantidadeRecebida < 0)
            throw new ArgumentException("Quantidade recebida não pode ser negativa.");

        QuantidadeRecebida = quantidadeRecebida;
        QuantidadeDivergente = Quantidade - quantidadeRecebida;
        ObservacaoRecebimento = observacao?.Trim();
    }
}
