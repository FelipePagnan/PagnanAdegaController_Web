using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Commands.Vendas;

public record CriarDevolucaoCommand(DevolucaoRequest Request, Guid UsuarioId) : IRequest<Result<Guid>>;

public class CriarDevolucaoHandler : IRequestHandler<CriarDevolucaoCommand, Result<Guid>>
{
    private readonly IVendaRepository _vendaRepo;
    private readonly IDevolucaoRepository _devolucaoRepo;
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;
    private readonly IUnitOfWork _uow;

    public CriarDevolucaoHandler(IVendaRepository vendaRepo, IDevolucaoRepository devolucaoRepo,
        IEstoqueProdutoRepository estoqueRepo, IMovimentacaoEstoqueRepository movRepo, IUnitOfWork uow)
    {
        _vendaRepo = vendaRepo; _devolucaoRepo = devolucaoRepo;
        _estoqueRepo = estoqueRepo; _movRepo = movRepo; _uow = uow;
    }

    public async Task<Result<Guid>> Handle(CriarDevolucaoCommand cmd, CancellationToken ct)
    {
        var venda = await _vendaRepo.ObterComDetalhesAsync(cmd.Request.VendaId, ct);
        if (venda is null)
            return Result.Fail<Guid>("Venda não encontrada.", "VENDA_NAO_ENCONTRADA");

        if (venda.Status != StatusVenda.Finalizada)
            return Result.Fail<Guid>("Apenas vendas finalizadas podem ter devolução.", "STATUS_INVALIDO");

        var devolucao = Devolucao.Criar(cmd.Request.VendaId, cmd.UsuarioId, cmd.Request.Motivo);

        foreach (var item in cmd.Request.Itens)
        {
            // Validar que o item pertence à venda
            var itemVenda = venda.Itens.FirstOrDefault(i => i.ProdutoId == item.ProdutoId);
            if (itemVenda is null)
                return Result.Fail<Guid>($"Produto não faz parte desta venda.", "ITEM_INVALIDO");

            devolucao.AdicionarItem(item.ProdutoId, item.Quantidade, item.PrecoUnitario);

            // Devolver ao estoque
            var estoque = await _estoqueRepo.ObterAsync(item.ProdutoId, venda.FilialId, ct);
            if (estoque != null)
            {
                var saldoAnterior = estoque.EstoqueFisico;
                estoque.Entrada(item.Quantidade);

                await _movRepo.AdicionarAsync(MovimentacaoEstoque.Criar(
                    item.ProdutoId, venda.FilialId, TipoMovimentacao.Devolucao,
                    item.Quantidade, saldoAnterior, estoque.EstoqueFisico,
                    cmd.UsuarioId,
                    motivo: $"Devolução venda #{venda.Numero}: {cmd.Request.Motivo}",
                    documentoOrigem: $"Venda #{venda.Numero}"), ct);
            }
        }

        await _devolucaoRepo.AdicionarAsync(devolucao, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Ok(devolucao.Id);
    }
}
