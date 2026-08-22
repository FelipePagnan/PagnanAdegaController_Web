using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Exceptions;

namespace ERP.Adega.Domain.Entities;

/// <summary>
/// Saldo de estoque por produto e filial.
/// RN-001: Estoque não pode ficar negativo.
/// RN-002: Físico, reservado e disponível são diferentes.
/// RN-004: Venda utiliza estoque disponível.
/// </summary>
public class EstoqueProduto : EntityBase
{
    public Guid ProdutoId { get; private set; }
    public Guid FilialId { get; private set; }
    public int EstoqueFisico { get; private set; }
    public int EstoqueReservado { get; private set; }
    public string? LocalizacaoFisica { get; private set; }

    // Navegação
    public Produto Produto { get; private set; } = default!;
    public Filial Filial { get; private set; } = default!;

    /// <summary>
    /// RN-002: Disponível = Físico - Reservado
    /// </summary>
    public int EstoqueDisponivel => EstoqueFisico - EstoqueReservado;

    private EstoqueProduto() { }

    public static EstoqueProduto Criar(Guid produtoId, Guid filialId, string? localizacao = null)
    {
        return new EstoqueProduto
        {
            ProdutoId = produtoId,
            FilialId = filialId,
            EstoqueFisico = 0,
            EstoqueReservado = 0,
            LocalizacaoFisica = localizacao
        };
    }

    /// <summary>
    /// Adiciona quantidade ao estoque físico (entrada, devolução).
    /// RN-003: Gera movimentação (responsabilidade do serviço).
    /// </summary>
    public void Entrada(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade de entrada deve ser positiva.", nameof(quantidade));

        EstoqueFisico += quantidade;
        MarcarAtualizado();
    }

    /// <summary>
    /// Remove quantidade do estoque físico (venda, perda, dano).
    /// RN-001: Bloqueia se resultaria em estoque negativo.
    /// RN-004: Só pode consumir estoque disponível.
    /// </summary>
    public void Saida(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade de saída deve ser positiva.", nameof(quantidade));

        if (quantidade > EstoqueDisponivel)
            throw new EstoqueInsuficienteException(ProdutoId, quantidade, EstoqueDisponivel);

        EstoqueFisico -= quantidade;
        ValidarInvariantes();
        MarcarAtualizado();
    }

    /// <summary>
    /// RN-013: Reserva reduz estoque disponível.
    /// </summary>
    public void Reservar(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade de reserva deve ser positiva.", nameof(quantidade));

        if (quantidade > EstoqueDisponivel)
            throw new EstoqueInsuficienteException(ProdutoId, quantidade, EstoqueDisponivel);

        EstoqueReservado += quantidade;
        ValidarInvariantes();
        MarcarAtualizado();
    }

    /// <summary>
    /// Libera reserva (expiração, cancelamento, retirada).
    /// Na retirada, o estoque físico será reduzido separadamente.
    /// </summary>
    public void LiberarReserva(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser positiva.", nameof(quantidade));

        if (quantidade > EstoqueReservado)
            throw new DomainException("RESERVA_INVALIDA", "Quantidade a liberar excede o reservado.");

        EstoqueReservado -= quantidade;
        MarcarAtualizado();
    }

    /// <summary>
    /// Ajuste de inventário. Pode aumentar ou diminuir.
    /// RN-010: Divergência gera ajuste rastreável.
    /// </summary>
    public void AjustarInventario(int novoEstoqueFisico)
    {
        if (novoEstoqueFisico < 0)
            throw new EstoqueNegativoException();

        if (novoEstoqueFisico < EstoqueReservado)
            throw new DomainException("AJUSTE_INVALIDO",
                $"Estoque físico ({novoEstoqueFisico}) não pode ser menor que o reservado ({EstoqueReservado}).");

        EstoqueFisico = novoEstoqueFisico;
        MarcarAtualizado();
    }

    public void AtualizarLocalizacao(string? localizacao)
    {
        LocalizacaoFisica = localizacao?.Trim();
        MarcarAtualizado();
    }

    /// <summary>
    /// Calcula nível de alerta baseado nas configurações do produto.
    /// </summary>
    public NivelAlertaEstoque CalcularAlerta(Produto produto)
        => produto.CalcularAlerta(EstoqueDisponivel);

    /// <summary>
    /// Formata o estoque em fardos + unidades para exibição.
    /// Ex: 52 unidades com fardo de 12 = "4 fardos + 4 un"
    /// </summary>
    public (int fardos, int unidades) CalcularFardosUnidades(int quantidadePorFardo)
    {
        if (quantidadePorFardo <= 0) return (0, EstoqueFisico);
        return (EstoqueFisico / quantidadePorFardo, EstoqueFisico % quantidadePorFardo);
    }

    private void ValidarInvariantes()
    {
        if (EstoqueFisico < 0)
            throw new EstoqueNegativoException();

        if (EstoqueDisponivel < 0)
            throw new EstoqueNegativoException();
    }
}
