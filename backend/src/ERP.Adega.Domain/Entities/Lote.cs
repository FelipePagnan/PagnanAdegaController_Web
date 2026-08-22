using ERP.Adega.Domain.Exceptions;

namespace ERP.Adega.Domain.Entities;

/// <summary>
/// Rastreabilidade de lote com validade.
/// RN-008: Validade vinculada ao lote.
/// RN-009: FEFO prioriza vencimento mais próximo.
/// </summary>
public class Lote : EntityBase
{
    public Guid ProdutoId { get; private set; }
    public Guid FilialId { get; private set; }
    public string Codigo { get; private set; } = default!;
    public DateTime? DataFabricacao { get; private set; }
    public DateTime? DataValidade { get; private set; }
    public Guid? FornecedorId { get; private set; }
    public string? NotaFiscal { get; private set; }
    public decimal CustoUnitario { get; private set; }
    public int QuantidadeRecebida { get; private set; }
    public int QuantidadeAtual { get; private set; }

    // Navegação
    public Produto Produto { get; private set; } = default!;
    public Filial Filial { get; private set; } = default!;
    public Fornecedor? Fornecedor { get; private set; }

    private Lote() { }

    public static Lote Criar(
        Guid produtoId,
        Guid filialId,
        string codigo,
        int quantidade,
        decimal custoUnitario,
        DateTime? dataValidade = null,
        DateTime? dataFabricacao = null,
        Guid? fornecedorId = null,
        string? notaFiscal = null)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));

        return new Lote
        {
            ProdutoId = produtoId,
            FilialId = filialId,
            Codigo = codigo.Trim(),
            QuantidadeRecebida = quantidade,
            QuantidadeAtual = quantidade,
            CustoUnitario = custoUnitario,
            DataValidade = dataValidade,
            DataFabricacao = dataFabricacao,
            FornecedorId = fornecedorId,
            NotaFiscal = notaFiscal?.Trim()
        };
    }

    /// <summary>
    /// Verifica se o lote está vencido.
    /// </summary>
    public bool EstaVencido() => DataValidade.HasValue && DataValidade.Value.Date < DateTime.UtcNow.Date;

    /// <summary>
    /// Verifica se está próximo do vencimento (dentro dos dias informados).
    /// </summary>
    public bool EstaVencendo(int diasAlerta = 30)
        => DataValidade.HasValue &&
           !EstaVencido() &&
           DataValidade.Value.Date <= DateTime.UtcNow.Date.AddDays(diasAlerta);

    /// <summary>
    /// Consome quantidade do lote. Usado no FEFO.
    /// </summary>
    public void Consumir(int quantidade)
    {
        if (EstaVencido())
            throw new LoteVencidoException(Codigo);

        if (quantidade > QuantidadeAtual)
            throw new EstoqueInsuficienteException(ProdutoId, quantidade, QuantidadeAtual);

        QuantidadeAtual -= quantidade;
        MarcarAtualizado();
    }

    /// <summary>
    /// Devolve quantidade ao lote.
    /// </summary>
    public void Devolver(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser positiva.", nameof(quantidade));

        QuantidadeAtual += quantidade;
        MarcarAtualizado();
    }

    public bool PossuiEstoque() => QuantidadeAtual > 0;
}
