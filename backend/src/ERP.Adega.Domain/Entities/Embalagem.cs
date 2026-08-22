namespace ERP.Adega.Domain.Entities;

/// <summary>
/// Representa uma embalagem do produto (fardo, caixa, pack).
/// RN-007: Quantidade por embalagem é configurável por produto.
/// O sistema NÃO assume que todo fardo possui 12 unidades.
/// </summary>
public class Embalagem : EntityBase
{
    public Guid ProdutoId { get; private set; }
    public string Nome { get; private set; } = default!;
    public int QuantidadeUnidades { get; private set; }
    public string? CodigoBarras { get; private set; }
    public decimal? PrecoSugerido { get; private set; }

    // Navegação
    public Produto Produto { get; private set; } = default!;

    private Embalagem() { }

    internal Embalagem(Guid produtoId, string nome, int quantidadeUnidades,
        string? codigoBarras, decimal? precoSugerido)
    {
        ProdutoId = produtoId;
        Nome = nome;
        QuantidadeUnidades = quantidadeUnidades;
        CodigoBarras = codigoBarras;
        PrecoSugerido = precoSugerido;
    }

    public void Atualizar(string nome, int quantidadeUnidades, string? codigoBarras, decimal? precoSugerido)
    {
        Nome = nome.Trim();
        QuantidadeUnidades = quantidadeUnidades;
        CodigoBarras = codigoBarras?.Trim();
        PrecoSugerido = precoSugerido;
    }

    /// <summary>
    /// Converte quantidade de embalagens para unidade base.
    /// Ex: 3 fardos de 12 = 36 unidades base.
    /// </summary>
    public int ConverterParaUnidadeBase(int quantidadeEmbalagens)
        => quantidadeEmbalagens * QuantidadeUnidades;
}
