using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Exceptions;

namespace ERP.Adega.Domain.Entities;

/// <summary>
/// Aggregate root do cadastro de produtos.
/// RN-005: UnidadeBase é a unidade comercializável.
/// RN-006: Produto fechado não pode ser fracionado.
/// RN-007: Quantidade por embalagem é configurável por produto.
/// RN-017: Com histórico, nunca excluir fisicamente.
/// </summary>
public class Produto : EntityBase
{
    public string Nome { get; private set; } = default!;
    public string? Descricao { get; private set; }
    public Guid CategoriaId { get; private set; }
    public UnidadeBase UnidadeBase { get; private set; }
    public bool ControlaValidade { get; private set; }
    public int? EstoqueMinimo { get; private set; }
    public int? EstoqueCritico { get; private set; }
    public decimal PrecoVenda { get; private set; }
    public decimal? PrecoCusto { get; private set; }
    public bool Ativo { get; private set; } = true;

    // Navegação
    public Categoria Categoria { get; private set; } = default!;

    private readonly List<CodigoBarras> _codigosBarras = new();
    public IReadOnlyCollection<CodigoBarras> CodigosBarras => _codigosBarras.AsReadOnly();

    private readonly List<Embalagem> _embalagens = new();
    public IReadOnlyCollection<Embalagem> Embalagens => _embalagens.AsReadOnly();

    private Produto() { }

    public static Produto Criar(
        string nome,
        Guid categoriaId,
        UnidadeBase unidadeBase,
        decimal precoVenda,
        bool controlaValidade = false,
        string? descricao = null,
        int? estoqueMinimo = null,
        int? estoqueCritico = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome do produto é obrigatório.", nameof(nome));

        if (precoVenda < 0)
            throw new ArgumentException("Preço de venda não pode ser negativo.", nameof(precoVenda));

        if (estoqueCritico.HasValue && estoqueMinimo.HasValue && estoqueCritico > estoqueMinimo)
            throw new ArgumentException("Estoque crítico não pode ser maior que o mínimo.", nameof(estoqueCritico));

        return new Produto
        {
            Nome = nome.Trim(),
            Descricao = descricao?.Trim(),
            CategoriaId = categoriaId,
            UnidadeBase = unidadeBase,
            PrecoVenda = precoVenda,
            ControlaValidade = controlaValidade,
            EstoqueMinimo = estoqueMinimo,
            EstoqueCritico = estoqueCritico
        };
    }

    public void Atualizar(string nome, string? descricao, Guid categoriaId,
        decimal precoVenda, bool controlaValidade, int? estoqueMinimo, int? estoqueCritico)
    {
        Nome = nome.Trim();
        Descricao = descricao?.Trim();
        CategoriaId = categoriaId;
        PrecoVenda = precoVenda;
        ControlaValidade = controlaValidade;
        EstoqueMinimo = estoqueMinimo;
        EstoqueCritico = estoqueCritico;
        MarcarAtualizado();
    }

    public void AtualizarPrecoCusto(decimal precoCusto)
    {
        PrecoCusto = precoCusto;
        MarcarAtualizado();
    }

    public void Inativar()
    {
        Ativo = false;
        MarcarAtualizado();
    }

    public void Ativar()
    {
        Ativo = true;
        MarcarAtualizado();
    }

    public void ValidarAtivo()
    {
        if (!Ativo)
            throw new ProdutoInativoException(Id);
    }

    // --- Códigos de barras ---

    public CodigoBarras AdicionarCodigoBarras(string codigo, TipoCodigoBarras tipo, bool principal = false)
    {
        if (principal)
        {
            foreach (var cb in _codigosBarras.Where(c => c.Principal))
                cb.DesmarcarPrincipal();
        }

        var codigoBarras = new CodigoBarras(Id, codigo.Trim(), tipo, principal);
        _codigosBarras.Add(codigoBarras);
        return codigoBarras;
    }

    // --- Embalagens ---

    /// <summary>
    /// RN-007: Quantidade por embalagem é configurável por produto.
    /// Ex: Red Bull → fardo de 4, Coca-Cola → fardo de 12.
    /// </summary>
    public Embalagem AdicionarEmbalagem(string nome, int quantidadeUnidades,
        string? codigoBarras = null, decimal? precoSugerido = null)
    {
        if (quantidadeUnidades <= 0)
            throw new ArgumentException("Quantidade de unidades deve ser maior que zero.", nameof(quantidadeUnidades));

        var embalagem = new Embalagem(Id, nome.Trim(), quantidadeUnidades, codigoBarras?.Trim(), precoSugerido);
        _embalagens.Add(embalagem);
        return embalagem;
    }

    /// <summary>
    /// Calcula nível de alerta baseado no estoque disponível.
    /// </summary>
    public NivelAlertaEstoque CalcularAlerta(int estoqueDisponivel)
    {
        if (EstoqueCritico.HasValue && estoqueDisponivel <= EstoqueCritico.Value)
            return NivelAlertaEstoque.Critico;

        if (EstoqueMinimo.HasValue && estoqueDisponivel <= EstoqueMinimo.Value)
            return NivelAlertaEstoque.Baixo;

        return NivelAlertaEstoque.Normal;
    }
}
