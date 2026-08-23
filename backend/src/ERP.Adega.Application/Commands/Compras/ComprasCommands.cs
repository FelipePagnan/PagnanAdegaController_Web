using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Commands.Compras;

// ═══════════════════════════════════════
// CRIAR PEDIDO DE COMPRA
// ═══════════════════════════════════════
public record CriarPedidoCompraCommand(CriarPedidoCompraRequest Request, Guid UsuarioId) : IRequest<Result<Guid>>;

public class CriarPedidoCompraHandler : IRequestHandler<CriarPedidoCompraCommand, Result<Guid>>
{
    private readonly IPedidoCompraRepository _pedidoRepo;
    private readonly IFornecedorRepository _fornecedorRepo;
    private readonly IUnitOfWork _uow;

    public CriarPedidoCompraHandler(IPedidoCompraRepository pedidoRepo,
        IFornecedorRepository fornecedorRepo, IUnitOfWork uow)
    {
        _pedidoRepo = pedidoRepo;
        _fornecedorRepo = fornecedorRepo;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(CriarPedidoCompraCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        var fornecedor = await _fornecedorRepo.ObterPorIdAsync(req.FornecedorId, ct);
        if (fornecedor is null)
            return Result.Fail<Guid>("Fornecedor não encontrado.", "FORNECEDOR_NAO_ENCONTRADO");

        if (req.Itens == null || req.Itens.Count == 0)
            return Result.Fail<Guid>("Pedido deve ter pelo menos um item.", "PEDIDO_VAZIO");

        var numero = await _pedidoRepo.ProximoNumeroAsync(req.FilialId, ct);

        var pedido = PedidoCompra.Criar(numero, req.FornecedorId, req.FilialId, cmd.UsuarioId,
            req.Frete, req.Desconto, req.Observacoes);

        foreach (var item in req.Itens)
        {
            pedido.AdicionarItem(item.ProdutoId, item.ProdutoNome, item.Quantidade,
                item.PrecoUnitario, item.CodigoLote, item.DataValidade);
        }

        // Limite de auto-aprovação: R$ 5.000 (parametrizável futuramente)
        pedido.EnviarParaAprovacao(5000m);

        await _pedidoRepo.AdicionarAsync(pedido, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Ok(pedido.Id);
    }
}

// ═══════════════════════════════════════
// APROVAR PEDIDO
// ═══════════════════════════════════════
public record AprovarPedidoCommand(Guid PedidoId, Guid AprovadorId) : IRequest<Result>;

public class AprovarPedidoHandler : IRequestHandler<AprovarPedidoCommand, Result>
{
    private readonly IPedidoCompraRepository _repo;
    private readonly IUnitOfWork _uow;

    public AprovarPedidoHandler(IPedidoCompraRepository repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<Result> Handle(AprovarPedidoCommand cmd, CancellationToken ct)
    {
        var pedido = await _repo.ObterPorIdAsync(cmd.PedidoId, ct);
        if (pedido is null) return Result.Fail("Pedido não encontrado.", "PEDIDO_NAO_ENCONTRADO");

        pedido.Aprovar(cmd.AprovadorId);
        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

// ═══════════════════════════════════════
// REJEITAR PEDIDO
// ═══════════════════════════════════════
public record RejeitarPedidoCommand(Guid PedidoId, Guid AprovadorId, string Motivo) : IRequest<Result>;

public class RejeitarPedidoHandler : IRequestHandler<RejeitarPedidoCommand, Result>
{
    private readonly IPedidoCompraRepository _repo;
    private readonly IUnitOfWork _uow;

    public RejeitarPedidoHandler(IPedidoCompraRepository repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<Result> Handle(RejeitarPedidoCommand cmd, CancellationToken ct)
    {
        var pedido = await _repo.ObterPorIdAsync(cmd.PedidoId, ct);
        if (pedido is null) return Result.Fail("Pedido não encontrado.", "PEDIDO_NAO_ENCONTRADO");

        pedido.Rejeitar(cmd.AprovadorId, cmd.Motivo);
        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

// ═══════════════════════════════════════
// RECEBER PEDIDO (entrada no estoque)
// ═══════════════════════════════════════
public record ReceberPedidoCommand(Guid PedidoId, ReceberPedidoRequest Request, Guid UsuarioId) : IRequest<Result>;

public class ReceberPedidoHandler : IRequestHandler<ReceberPedidoCommand, Result>
{
    private readonly IPedidoCompraRepository _pedidoRepo;
    private readonly IProdutoRepository _produtoRepo;
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly ILoteRepository _loteRepo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;
    private readonly IUnitOfWork _uow;

    public ReceberPedidoHandler(IPedidoCompraRepository pedidoRepo, IProdutoRepository produtoRepo,
        IEstoqueProdutoRepository estoqueRepo, ILoteRepository loteRepo,
        IMovimentacaoEstoqueRepository movRepo, IUnitOfWork uow)
    {
        _pedidoRepo = pedidoRepo;
        _produtoRepo = produtoRepo;
        _estoqueRepo = estoqueRepo;
        _loteRepo = loteRepo;
        _movRepo = movRepo;
        _uow = uow;
    }

    public async Task<Result> Handle(ReceberPedidoCommand cmd, CancellationToken ct)
    {
        var pedido = await _pedidoRepo.ObterComDetalhesAsync(cmd.PedidoId, ct);
        if (pedido is null) return Result.Fail("Pedido não encontrado.", "PEDIDO_NAO_ENCONTRADO");

        // Registrar quantidades recebidas em cada item
        foreach (var recItem in cmd.Request.Itens)
        {
            var item = pedido.Itens.FirstOrDefault(i => i.Id == recItem.ItemId);
            if (item is null) continue;
            item.RegistrarRecebimento(recItem.QuantidadeRecebida, recItem.Observacao);
        }

        pedido.RegistrarRecebimento(cmd.Request.NotaFiscal);

        // Dar entrada no estoque para cada item recebido
        foreach (var item in pedido.Itens.Where(i => i.QuantidadeRecebida > 0))
        {
            // Criar lote
            var codigoLote = item.CodigoLote ?? $"PC{pedido.Numero}-{DateTime.UtcNow:yyyyMMdd}";
            var lote = Lote.Criar(item.ProdutoId, pedido.FilialId, codigoLote, item.QuantidadeRecebida,
                item.PrecoUnitario, item.DataValidade, fornecedorId: pedido.FornecedorId,
                notaFiscal: cmd.Request.NotaFiscal);
            await _loteRepo.AdicionarAsync(lote, ct);

            // Atualizar estoque
            var estoque = await _estoqueRepo.ObterAsync(item.ProdutoId, pedido.FilialId, ct);
            if (estoque is null)
            {
                estoque = EstoqueProduto.Criar(item.ProdutoId, pedido.FilialId);
                await _estoqueRepo.AdicionarAsync(estoque, ct);
            }

            var saldoAnterior = estoque.EstoqueFisico;
            estoque.Entrada(item.QuantidadeRecebida);

            // Movimentação
            await _movRepo.AdicionarAsync(MovimentacaoEstoque.Criar(
                item.ProdutoId, pedido.FilialId, TipoMovimentacao.Entrada,
                item.QuantidadeRecebida, saldoAnterior, estoque.EstoqueFisico,
                cmd.UsuarioId, lote.Id,
                documentoOrigem: $"Pedido #{pedido.Numero} / NF {cmd.Request.NotaFiscal}"), ct);

            // Atualizar custo do produto
            var produto = await _produtoRepo.ObterPorIdAsync(item.ProdutoId, ct);
            produto?.AtualizarPrecoCusto(item.PrecoUnitario);
        }

        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

// ═══════════════════════════════════════
// CANCELAR PEDIDO
// ═══════════════════════════════════════
public record CancelarPedidoCompraCommand(Guid PedidoId) : IRequest<Result>;

public class CancelarPedidoCompraHandler : IRequestHandler<CancelarPedidoCompraCommand, Result>
{
    private readonly IPedidoCompraRepository _repo;
    private readonly IUnitOfWork _uow;

    public CancelarPedidoCompraHandler(IPedidoCompraRepository repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<Result> Handle(CancelarPedidoCompraCommand cmd, CancellationToken ct)
    {
        var pedido = await _repo.ObterPorIdAsync(cmd.PedidoId, ct);
        if (pedido is null) return Result.Fail("Pedido não encontrado.", "PEDIDO_NAO_ENCONTRADO");

        pedido.Cancelar();
        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
