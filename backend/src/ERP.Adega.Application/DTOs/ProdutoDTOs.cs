using ERP.Adega.Domain.Enums;

namespace ERP.Adega.Application.DTOs;

public record ProdutoDto(
    Guid Id,
    string Nome,
    string? Descricao,
    Guid CategoriaId,
    string CategoriaNome,
    UnidadeBase UnidadeBase,
    bool ControlaValidade,
    int? EstoqueMinimo,
    int? EstoqueCritico,
    decimal PrecoVenda,
    decimal? PrecoCusto,
    bool Ativo,
    DateTime CriadoEm,
    DateTime? AtualizadoEm,
    IReadOnlyList<CodigoBarrasDto> CodigosBarras,
    IReadOnlyList<EmbalagemDto> Embalagens
);

public record ProdutoResumoDto(
    Guid Id,
    string Nome,
    string CategoriaNome,
    decimal PrecoVenda,
    bool Ativo
);

public record CodigoBarrasDto(
    Guid Id,
    string Codigo,
    TipoCodigoBarras Tipo,
    bool Principal
);

public record EmbalagemDto(
    Guid Id,
    string Nome,
    int QuantidadeUnidades,
    string? CodigoBarras,
    decimal? PrecoSugerido
);

public record CriarProdutoRequest(
    string Nome,
    string? Descricao,
    Guid CategoriaId,
    UnidadeBase UnidadeBase,
    decimal PrecoVenda,
    bool ControlaValidade = false,
    int? EstoqueMinimo = null,
    int? EstoqueCritico = null,
    List<CriarCodigoBarrasRequest>? CodigosBarras = null,
    List<CriarEmbalagemRequest>? Embalagens = null
);

public record CriarCodigoBarrasRequest(
    string Codigo,
    TipoCodigoBarras Tipo = TipoCodigoBarras.EAN13,
    bool Principal = false
);

public record CriarEmbalagemRequest(
    string Nome,
    int QuantidadeUnidades,
    string? CodigoBarras = null,
    decimal? PrecoSugerido = null
);

public record AtualizarProdutoRequest(
    string Nome,
    string? Descricao,
    Guid CategoriaId,
    decimal PrecoVenda,
    bool ControlaValidade,
    int? EstoqueMinimo,
    int? EstoqueCritico
);
