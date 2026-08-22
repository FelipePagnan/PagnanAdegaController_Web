using ERP.Adega.Application.DTOs;
using ERP.Adega.Application.Common;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Queries.Produtos;

public record ObterProdutoQuery(Guid Id) : IRequest<Result<ProdutoDto>>;

public class ObterProdutoQueryHandler : IRequestHandler<ObterProdutoQuery, Result<ProdutoDto>>
{
    private readonly IProdutoRepository _repo;

    public ObterProdutoQueryHandler(IProdutoRepository repo) => _repo = repo;

    public async Task<Result<ProdutoDto>> Handle(ObterProdutoQuery query, CancellationToken ct)
    {
        var p = await _repo.ObterComDetalhesAsync(query.Id, ct);
        if (p is null)
            return Result.Fail<ProdutoDto>("Produto não encontrado.", "PRODUTO_NAO_ENCONTRADO");

        var dto = new ProdutoDto(
            p.Id, p.Nome, p.Descricao, p.CategoriaId,
            p.Categoria?.Nome ?? "",
            p.UnidadeBase, p.ControlaValidade,
            p.EstoqueMinimo, p.EstoqueCritico,
            p.PrecoVenda, p.PrecoCusto, p.Ativo,
            p.CriadoEm, p.AtualizadoEm,
            p.CodigosBarras.Select(cb => new CodigoBarrasDto(cb.Id, cb.Codigo, cb.Tipo, cb.Principal)).ToList(),
            p.Embalagens.Select(e => new EmbalagemDto(e.Id, e.Nome, e.QuantidadeUnidades, e.CodigoBarras, e.PrecoSugerido)).ToList()
        );

        return Result.Ok(dto);
    }
}

public record BuscarPorBarcodeQuery(string Codigo) : IRequest<Result<ProdutoDto>>;

public class BuscarPorBarcodeQueryHandler : IRequestHandler<BuscarPorBarcodeQuery, Result<ProdutoDto>>
{
    private readonly IProdutoRepository _repo;

    public BuscarPorBarcodeQueryHandler(IProdutoRepository repo) => _repo = repo;

    public async Task<Result<ProdutoDto>> Handle(BuscarPorBarcodeQuery query, CancellationToken ct)
    {
        var p = await _repo.ObterPorCodigoBarrasAsync(query.Codigo, ct);
        if (p is null)
            return Result.Fail<ProdutoDto>("Produto não encontrado para este código de barras.", "BARCODE_NAO_ENCONTRADO");

        var dto = new ProdutoDto(
            p.Id, p.Nome, p.Descricao, p.CategoriaId,
            p.Categoria?.Nome ?? "",
            p.UnidadeBase, p.ControlaValidade,
            p.EstoqueMinimo, p.EstoqueCritico,
            p.PrecoVenda, p.PrecoCusto, p.Ativo,
            p.CriadoEm, p.AtualizadoEm,
            p.CodigosBarras.Select(cb => new CodigoBarrasDto(cb.Id, cb.Codigo, cb.Tipo, cb.Principal)).ToList(),
            p.Embalagens.Select(e => new EmbalagemDto(e.Id, e.Nome, e.QuantidadeUnidades, e.CodigoBarras, e.PrecoSugerido)).ToList()
        );

        return Result.Ok(dto);
    }
}
