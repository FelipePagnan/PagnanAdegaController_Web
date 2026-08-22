using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Exceptions;

namespace ERP.Adega.Domain.Entities;

/// <summary>
/// RN-003: Toda alteração de estoque gera movimentação rastreável.
/// RN-012: Motivo obrigatório em operações críticas.
/// Imutável após criação.
/// </summary>
public class MovimentacaoEstoque : EntityBase
{
    public Guid ProdutoId { get; private set; }
    public Guid FilialId { get; private set; }
    public Guid? LoteId { get; private set; }
    public TipoMovimentacao Tipo { get; private set; }
    public int Quantidade { get; private set; }
    public int SaldoAnterior { get; private set; }
    public int SaldoPosterior { get; private set; }
    public string? Motivo { get; private set; }
    public string? DocumentoOrigem { get; private set; }
    public Guid UsuarioId { get; private set; }

    // Navegação
    public Produto Produto { get; private set; } = default!;
    public Filial Filial { get; private set; } = default!;
    public Lote? Lote { get; private set; }
    public Usuario Usuario { get; private set; } = default!;

    private MovimentacaoEstoque() { }

    private static readonly TipoMovimentacao[] _tiposComMotivoObrigatorio =
    {
        TipoMovimentacao.Ajuste,
        TipoMovimentacao.Perda,
        TipoMovimentacao.Dano
    };

    public static MovimentacaoEstoque Criar(
        Guid produtoId,
        Guid filialId,
        TipoMovimentacao tipo,
        int quantidade,
        int saldoAnterior,
        int saldoPosterior,
        Guid usuarioId,
        Guid? loteId = null,
        string? motivo = null,
        string? documentoOrigem = null)
    {
        // RN-012: Motivo obrigatório em operações críticas
        if (_tiposComMotivoObrigatorio.Contains(tipo) && string.IsNullOrWhiteSpace(motivo))
            throw new OperacaoSemMotivoException(tipo.ToString());

        return new MovimentacaoEstoque
        {
            ProdutoId = produtoId,
            FilialId = filialId,
            LoteId = loteId,
            Tipo = tipo,
            Quantidade = quantidade,
            SaldoAnterior = saldoAnterior,
            SaldoPosterior = saldoPosterior,
            UsuarioId = usuarioId,
            Motivo = motivo?.Trim(),
            DocumentoOrigem = documentoOrigem?.Trim()
        };
    }
}
