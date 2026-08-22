using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Queries.Produtos;

public record ListarProdutosQuery(
    string? Termo,
    Guid? CategoriaId,
    bool? Ativo,
    int Pagina = 1,
    int TamanhoPagina = 20
) : IRequest<PagedResult<ProdutoResumoDto>>;

public class ListarProdutosQueryHandler
    : IRequestHandler<ListarProdutosQuery, PagedResult<ProdutoResumoDto>>
{
    private readonly IProdutoRepository _repo;

    public ListarProdutosQueryHandler(IProdutoRepository repo) => _repo = repo;

    public async Task<PagedResult<ProdutoResumoDto>> Handle(
        ListarProdutosQuery query, CancellationToken ct)
    {
        var total = await _repo.ContarAsync(query.Termo, query.CategoriaId, query.Ativo, ct);

        var produtos = await _repo.BuscarAsync(
            query.Termo, query.CategoriaId, query.Ativo,
            query.Pagina, query.TamanhoPagina, ct);

        var dtos = produtos.Select(p => new ProdutoResumoDto(
            p.Id,
            p.Nome,
            p.Categoria?.Nome ?? "",
            p.PrecoVenda,
            p.Ativo
        )).ToList();

        return new PagedResult<ProdutoResumoDto>(dtos, total, query.Pagina, query.TamanhoPagina);
    }
}
