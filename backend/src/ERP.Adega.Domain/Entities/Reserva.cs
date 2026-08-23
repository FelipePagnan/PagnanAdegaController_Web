using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Exceptions;

namespace ERP.Adega.Domain.Entities;

/// <summary>
/// RN-013: Reserva reduz estoque disponível.
/// RN-014: Exige adiantamento conforme configuração.
/// RN-015: Possui prazo de validade.
/// </summary>
public class Reserva : EntityBase
{
    public int Numero { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid FilialId { get; private set; }
    public StatusReserva Status { get; private set; }
    public decimal ValorTotal { get; private set; }
    public decimal ValorAdiantamento { get; private set; }
    public decimal ValorRestante { get; private set; }
    public DateTime DataLimite { get; private set; }
    public string? Observacoes { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTime? RetiradoEm { get; private set; }
    public string? MotivoCancelamento { get; private set; }

    // Navegação
    public Cliente Cliente { get; private set; } = default!;
    public Filial Filial { get; private set; } = default!;
    public Usuario Usuario { get; private set; } = default!;

    private readonly List<ItemReserva> _itens = new();
    public IReadOnlyCollection<ItemReserva> Itens => _itens.AsReadOnly();

    private Reserva() { }

    public static Reserva Criar(int numero, Guid clienteId, Guid filialId, Guid usuarioId,
        decimal valorAdiantamento, DateTime dataLimite, string? observacoes = null)
    {
        if (dataLimite <= DateTime.UtcNow)
            throw new ArgumentException("Data limite deve ser futura.", nameof(dataLimite));

        return new Reserva
        {
            Numero = numero,
            ClienteId = clienteId,
            FilialId = filialId,
            UsuarioId = usuarioId,
            Status = StatusReserva.Ativa,
            ValorAdiantamento = valorAdiantamento,
            ValorTotal = 0,
            ValorRestante = 0,
            DataLimite = dataLimite,
            Observacoes = observacoes?.Trim()
        };
    }

    public ItemReserva AdicionarItem(Guid produtoId, string produtoNome, int quantidade, decimal precoUnitario)
    {
        if (Status != StatusReserva.Ativa)
            throw new DomainException("RESERVA_FECHADA", "Reserva não está ativa.");

        var item = new ItemReserva(Id, produtoId, produtoNome, quantidade, precoUnitario);
        _itens.Add(item);
        RecalcularTotais();
        return item;
    }

    public void Retirar()
    {
        if (Status != StatusReserva.Ativa)
            throw new DomainException("STATUS_INVALIDO", "Apenas reservas ativas podem ser retiradas.");

        Status = StatusReserva.Retirada;
        RetiradoEm = DateTime.UtcNow;
        MarcarAtualizado();
    }

    public void Cancelar(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new OperacaoSemMotivoException("Cancelamento de reserva");

        if (Status != StatusReserva.Ativa)
            throw new DomainException("STATUS_INVALIDO", "Apenas reservas ativas podem ser canceladas.");

        Status = StatusReserva.Cancelada;
        MotivoCancelamento = motivo.Trim();
        MarcarAtualizado();
    }

    public void Expirar()
    {
        if (Status != StatusReserva.Ativa) return;
        Status = StatusReserva.Expirada;
        MarcarAtualizado();
    }

    public bool EstaExpirada() => Status == StatusReserva.Ativa && DateTime.UtcNow > DataLimite;

    private void RecalcularTotais()
    {
        ValorTotal = _itens.Sum(i => i.Total);
        ValorRestante = ValorTotal - ValorAdiantamento;
        if (ValorRestante < 0) ValorRestante = 0;
    }
}

public class ItemReserva : EntityBase
{
    public Guid ReservaId { get; private set; }
    public Guid ProdutoId { get; private set; }
    public string ProdutoNome { get; private set; } = default!;
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal Total { get; private set; }

    private ItemReserva() { }

    internal ItemReserva(Guid reservaId, Guid produtoId, string produtoNome, int quantidade, decimal precoUnitario)
    {
        ReservaId = reservaId;
        ProdutoId = produtoId;
        ProdutoNome = produtoNome;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        Total = quantidade * precoUnitario;
    }
}
