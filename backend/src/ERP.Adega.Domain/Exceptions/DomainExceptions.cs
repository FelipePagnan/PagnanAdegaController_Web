namespace ERP.Adega.Domain.Exceptions;

public class DomainException : Exception
{
    public string Codigo { get; }

    public DomainException(string codigo, string mensagem) : base(mensagem)
    {
        Codigo = codigo;
    }
}

public class EstoqueInsuficienteException : DomainException
{
    public Guid ProdutoId { get; }
    public int QuantidadeSolicitada { get; }
    public int QuantidadeDisponivel { get; }

    public EstoqueInsuficienteException(Guid produtoId, int solicitada, int disponivel)
        : base("ESTOQUE_INSUFICIENTE",
            $"Estoque insuficiente. Solicitado: {solicitada}, Disponível: {disponivel}")
    {
        ProdutoId = produtoId;
        QuantidadeSolicitada = solicitada;
        QuantidadeDisponivel = disponivel;
    }
}

public class EstoqueNegativoException : DomainException
{
    public EstoqueNegativoException()
        : base("ESTOQUE_NEGATIVO", "Operação resultaria em estoque negativo. Bloqueada conforme RN-001.")
    { }
}

public class ProdutoInativoException : DomainException
{
    public ProdutoInativoException(Guid produtoId)
        : base("PRODUTO_INATIVO", $"Produto {produtoId} está inativo e não pode ser utilizado nesta operação.")
    { }
}

public class LoteVencidoException : DomainException
{
    public LoteVencidoException(string codigoLote)
        : base("LOTE_VENCIDO", $"Lote {codigoLote} está vencido e não pode ser utilizado para saída.")
    { }
}

public class AprovacaoNecessariaException : DomainException
{
    public string TipoOperacao { get; }
    public decimal Valor { get; }

    public AprovacaoNecessariaException(string tipoOperacao, decimal valor)
        : base("APROVACAO_NECESSARIA",
            $"Operação '{tipoOperacao}' com valor R$ {valor:N2} requer aprovação.")
    {
        TipoOperacao = tipoOperacao;
        Valor = valor;
    }
}

public class OperacaoSemMotivoException : DomainException
{
    public OperacaoSemMotivoException(string operacao)
        : base("MOTIVO_OBRIGATORIO",
            $"Motivo é obrigatório para a operação '{operacao}' conforme RN-012.")
    { }
}
