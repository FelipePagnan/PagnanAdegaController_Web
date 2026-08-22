using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Queries.Vendas;

public record ListarVendasQuery(
    Guid FilialId,
    DateTime? Inicio,
    DateTime? Fim,
    int Pagina = 1,
    int TamanhoPagina = 20
) : IRequest<PagedResult<VendaResumoDto>>;

public class ListarVendasQueryHandler : IRequestHandler<ListarVendasQuery, PagedResult<VendaResumoDto>>
{
    private readonly IVendaRepository _repo;

    public ListarVendasQueryHandler(IVendaRepository repo) => _repo = repo;

    public async Task<PagedResult<VendaResumoDto>> Handle(ListarVendasQuery query, CancellationToken ct)
    {
        var total = await _repo.ContarAsync(query.FilialId, query.Inicio, query.Fim, ct);
        var vendas = await _repo.ListarAsync(query.FilialId, query.Inicio, query.Fim,
            query.Pagina, query.TamanhoPagina, ct);

        var dtos = vendas.Select(v => new VendaResumoDto(
            v.Id, v.Numero, v.Status, v.Total, v.TotalItens,
            v.Usuario?.Nome ?? "",
            v.Pagamentos.FirstOrDefault()?.Forma.ToString() ?? "—",
            v.CriadoEm
        )).ToList();

        return new PagedResult<VendaResumoDto>(dtos, total, query.Pagina, query.TamanhoPagina);
    }
}

public record ObterVendaQuery(Guid Id) : IRequest<Result<VendaDto>>;

public class ObterVendaQueryHandler : IRequestHandler<ObterVendaQuery, Result<VendaDto>>
{
    private readonly IVendaRepository _repo;

    public ObterVendaQueryHandler(IVendaRepository repo) => _repo = repo;

    public async Task<Result<VendaDto>> Handle(ObterVendaQuery query, CancellationToken ct)
    {
        var v = await _repo.ObterComDetalhesAsync(query.Id, ct);
        if (v is null)
            return Result.Fail<VendaDto>("Venda não encontrada.", "VENDA_NAO_ENCONTRADA");

        var dto = new VendaDto(
            v.Id, v.Numero, v.FilialId, v.ClienteId, v.Cliente?.Nome,
            v.Status, v.SubTotal, v.Desconto, v.Total, v.TotalPago, v.Troco, v.TotalItens,
            v.Usuario?.Nome ?? "", v.MotivoCancelamento, v.CriadoEm, v.FinalizadoEm,
            v.Itens.Select(i => new ItemVendaDto(
                i.Id, i.ProdutoId, i.ProdutoNome, i.Quantidade,
                i.PrecoUnitario, i.Total, i.EmbalagemNome, i.UnidadesPorEmbalagem)).ToList(),
            v.Pagamentos.Select(p => new PagamentoVendaDto(
                p.Id, p.Forma, p.Valor, p.TaxaPercentual, p.TaxaValor, p.ValorLiquido)).ToList()
        );

        return Result.Ok(dto);
    }
}
