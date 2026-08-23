using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Exceptions;

namespace ERP.Adega.Domain.Entities;

/// <summary>
/// Transferência de estoque entre filiais.
/// Fluxo: Solicitada → Aprovada → Separada → Enviada → Recebida
/// </summary>
public class Transferencia : EntityBase
{
    public int Numero { get; private set; }
    public Guid FilialOrigemId { get; private set; }
    public Guid FilialDestinoId { get; private set; }
    public StatusTransferencia Status { get; private set; }
    public string? Observacoes { get; private set; }
    public Guid SolicitanteId { get; private set; }
    public Guid? AprovadoPorId { get; private set; }
    public DateTime? AprovadoEm { get; private set; }
    public DateTime? EnviadoEm { get; private set; }
    public DateTime? RecebidoEm { get; private set; }
    public string? MotivoCancelamento { get; private set; }

    // Navegação
    public Filial FilialOrigem { get; private set; } = default!;
    public Filial FilialDestino { get; private set; } = default!;
    public Usuario Solicitante { get; private set; } = default!;

    private readonly List<ItemTransferencia> _itens = new();
    public IReadOnlyCollection<ItemTransferencia> Itens => _itens.AsReadOnly();

    private Transferencia() { }

    public static Transferencia Criar(int numero, Guid filialOrigemId, Guid filialDestinoId,
        Guid solicitanteId, string? observacoes = null)
    {
        if (filialOrigemId == filialDestinoId)
            throw new DomainException("FILIAIS_IGUAIS", "Filial de origem e destino devem ser diferentes.");

        return new Transferencia
        {
            Numero = numero,
            FilialOrigemId = filialOrigemId,
            FilialDestinoId = filialDestinoId,
            SolicitanteId = solicitanteId,
            Status = StatusTransferencia.Solicitada,
            Observacoes = observacoes?.Trim()
        };
    }

    public ItemTransferencia AdicionarItem(Guid produtoId, string produtoNome, int quantidade)
    {
        if (Status != StatusTransferencia.Solicitada)
            throw new DomainException("STATUS_INVALIDO", "Itens só podem ser adicionados em transferências solicitadas.");

        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser positiva.");

        var item = new ItemTransferencia(Id, produtoId, produtoNome, quantidade);
        _itens.Add(item);
        return item;
    }

    public void Aprovar(Guid aprovadorId)
    {
        if (Status != StatusTransferencia.Solicitada)
            throw new DomainException("STATUS_INVALIDO", "Transferência não está solicitada.");
        Status = StatusTransferencia.Aprovada;
        AprovadoPorId = aprovadorId;
        AprovadoEm = DateTime.UtcNow;
        MarcarAtualizado();
    }

    public void MarcarSeparada()
    {
        if (Status != StatusTransferencia.Aprovada)
            throw new DomainException("STATUS_INVALIDO", "Transferência não está aprovada.");
        Status = StatusTransferencia.Separada;
        MarcarAtualizado();
    }

    public void MarcarEnviada()
    {
        if (Status != StatusTransferencia.Separada)
            throw new DomainException("STATUS_INVALIDO", "Transferência não está separada.");
        Status = StatusTransferencia.Enviada;
        EnviadoEm = DateTime.UtcNow;
        MarcarAtualizado();
    }

    public void RegistrarRecebimento()
    {
        if (Status != StatusTransferencia.Enviada)
            throw new DomainException("STATUS_INVALIDO", "Transferência não foi enviada.");
        Status = StatusTransferencia.Recebida;
        RecebidoEm = DateTime.UtcNow;
        MarcarAtualizado();
    }

    public void Cancelar(string motivo)
    {
        if (Status == StatusTransferencia.Recebida)
            throw new DomainException("JA_RECEBIDA", "Transferência já foi recebida.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new OperacaoSemMotivoException("Cancelamento de transferência");

        Status = StatusTransferencia.Cancelada;
        MotivoCancelamento = motivo.Trim();
        MarcarAtualizado();
    }

    public int TotalItens => _itens.Sum(i => i.Quantidade);
}

public class ItemTransferencia : EntityBase
{
    public Guid TransferenciaId { get; private set; }
    public Guid ProdutoId { get; private set; }
    public string ProdutoNome { get; private set; } = default!;
    public int Quantidade { get; private set; }
    public int QuantidadeRecebida { get; private set; }

    private ItemTransferencia() { }

    internal ItemTransferencia(Guid transferenciaId, Guid produtoId, string produtoNome, int quantidade)
    {
        TransferenciaId = transferenciaId;
        ProdutoId = produtoId;
        ProdutoNome = produtoNome;
        Quantidade = quantidade;
        QuantidadeRecebida = 0;
    }

    public void RegistrarRecebimento(int qtd) { QuantidadeRecebida = qtd; }
}
