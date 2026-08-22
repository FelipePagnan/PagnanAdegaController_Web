using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Queries.Compras;

public record ListarPedidosCompraQuery(
    Guid FilialId,
    StatusPedidoCompra? Status,
    int Pagina = 1,
    int TamanhoPagina = 20
) : IRequest<PagedResult<PedidoCompraResumoDto>>;

public class ListarPedidosHandler : IRequestHandler<ListarPedidosCompraQuery, PagedResult<PedidoCompraResumoDto>>
{
    private readonly IPedidoCompraRepository _repo;
    public ListarPedidosHandler(IPedidoCompraRepository repo) => _repo = repo;

    public async Task<PagedResult<PedidoCompraResumoDto>> Handle(ListarPedidosCompraQuery query, CancellationToken ct)
    {
        var total = await _repo.ContarAsync(query.FilialId, query.Status, ct);
        var pedidos = await _repo.ListarAsync(query.FilialId, query.Status, query.Pagina, query.TamanhoPagina, ct);

        var dtos = pedidos.Select(p => new PedidoCompraResumoDto(
            p.Id, p.Numero, p.Fornecedor?.RazaoSocial ?? "",
            p.Status, p.Total, p.Itens.Count,
            p.Usuario?.Nome ?? "", p.CriadoEm
        )).ToList();

        return new PagedResult<PedidoCompraResumoDto>(dtos, total, query.Pagina, query.TamanhoPagina);
    }
}

public record ObterPedidoCompraQuery(Guid Id) : IRequest<Result<PedidoCompraDto>>;

public class ObterPedidoHandler : IRequestHandler<ObterPedidoCompraQuery, Result<PedidoCompraDto>>
{
    private readonly IPedidoCompraRepository _repo;
    public ObterPedidoHandler(IPedidoCompraRepository repo) => _repo = repo;

    public async Task<Result<PedidoCompraDto>> Handle(ObterPedidoCompraQuery query, CancellationToken ct)
    {
        var p = await _repo.ObterComDetalhesAsync(query.Id, ct);
        if (p is null) return Result.Fail<PedidoCompraDto>("Pedido não encontrado.", "PEDIDO_NAO_ENCONTRADO");

        var dto = new PedidoCompraDto(
            p.Id, p.Numero, p.FornecedorId, p.Fornecedor?.RazaoSocial ?? "",
            p.FilialId, p.Status, p.SubTotal, p.Frete, p.Desconto, p.Total,
            p.Observacoes, p.NotaFiscal, p.Usuario?.Nome ?? "",
            null, p.AprovadoEm, p.MotivoRejeicao, p.RecebidoEm, p.CriadoEm,
            p.Itens.Select(i => new ItemCompraDto(
                i.Id, i.ProdutoId, i.ProdutoNome, i.Quantidade, i.PrecoUnitario, i.Total,
                i.QuantidadeRecebida, i.QuantidadeDivergente,
                i.CodigoLote, i.DataValidade, i.ObservacaoRecebimento)).ToList()
        );

        return Result.Ok(dto);
    }
}
