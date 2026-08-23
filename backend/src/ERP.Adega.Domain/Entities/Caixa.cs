using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Exceptions;

namespace ERP.Adega.Domain.Entities;

public class Caixa : EntityBase
{
    public Guid FilialId { get; private set; }
    public int Numero { get; private set; }
    public Guid UsuarioId { get; private set; }
    public StatusCaixa Status { get; private set; }
    public decimal SaldoAbertura { get; private set; }
    public decimal TotalEntradas { get; private set; }
    public decimal TotalSaidas { get; private set; }
    public decimal SaldoFechamento { get; private set; }
    public string? ObservacaoFechamento { get; private set; }
    public DateTime? FechadoEm { get; private set; }

    // Navegação
    public Filial Filial { get; private set; } = default!;
    public Usuario Usuario { get; private set; } = default!;

    private Caixa() { }

    public static Caixa Abrir(Guid filialId, int numero, Guid usuarioId, decimal saldoAbertura)
    {
        if (saldoAbertura < 0)
            throw new ArgumentException("Saldo de abertura não pode ser negativo.");

        return new Caixa
        {
            FilialId = filialId,
            Numero = numero,
            UsuarioId = usuarioId,
            Status = StatusCaixa.Aberto,
            SaldoAbertura = saldoAbertura,
            TotalEntradas = 0,
            TotalSaidas = 0,
            SaldoFechamento = 0
        };
    }

    public void RegistrarEntrada(decimal valor)
    {
        if (Status != StatusCaixa.Aberto)
            throw new DomainException("CAIXA_FECHADO", "Caixa está fechado.");
        TotalEntradas += valor;
    }

    public void RegistrarSaida(decimal valor)
    {
        if (Status != StatusCaixa.Aberto)
            throw new DomainException("CAIXA_FECHADO", "Caixa está fechado.");
        TotalSaidas += valor;
    }

    public decimal SaldoAtual => SaldoAbertura + TotalEntradas - TotalSaidas;

    public void Fechar(string? observacao = null)
    {
        if (Status != StatusCaixa.Aberto)
            throw new DomainException("CAIXA_FECHADO", "Caixa já está fechado.");

        SaldoFechamento = SaldoAtual;
        ObservacaoFechamento = observacao?.Trim();
        Status = StatusCaixa.Fechado;
        FechadoEm = DateTime.UtcNow;
        MarcarAtualizado();
    }
}
