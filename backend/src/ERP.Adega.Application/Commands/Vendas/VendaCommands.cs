using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Commands.Vendas;

// ═══════════════════════════════════════════════════
// CRIAR VENDA (PDV)
// Valida estoque, baixa FEFO, registra pagamentos
// ═══════════════════════════════════════════════════
public record CriarVendaCommand(CriarVendaRequest Request, Guid UsuarioId) : IRequest<Result<VendaDto>>;

public class CriarVendaCommandHandler : IRequestHandler<CriarVendaCommand, Result<VendaDto>>
{
    private readonly IVendaRepository _vendaRepo;
    private readonly IProdutoRepository _produtoRepo;
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly ILoteRepository _loteRepo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;
    private readonly IUnitOfWork _uow;

    public CriarVendaCommandHandler(
        IVendaRepository vendaRepo, IProdutoRepository produtoRepo,
        IEstoqueProdutoRepository estoqueRepo, ILoteRepository loteRepo,
        IMovimentacaoEstoqueRepository movRepo, IUnitOfWork uow)
    {
        _vendaRepo = vendaRepo;
        _produtoRepo = produtoRepo;
        _estoqueRepo = estoqueRepo;
        _loteRepo = loteRepo;
        _movRepo = movRepo;
        _uow = uow;
    }

    public async Task<Result<VendaDto>> Handle(CriarVendaCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        if (req.Itens == null || req.Itens.Count == 0)
            return Result.Fail<VendaDto>("Venda deve ter pelo menos um item.", "VENDA_VAZIA");

        // Gerar número sequencial
        var numero = await _vendaRepo.ProximoNumeroAsync(req.FilialId, ct);

        // Criar venda (CaixaId placeholder — será implementado no módulo Caixa)
        var venda = Venda.Criar(numero, req.FilialId, Guid.Empty, cmd.UsuarioId, req.ClienteId);

        // Adicionar itens e validar estoque
        foreach (var itemReq in req.Itens)
        {
            var produto = await _produtoRepo.ObterComDetalhesAsync(itemReq.ProdutoId, ct);
            if (produto is null)
                return Result.Fail<VendaDto>($"Produto não encontrado.", "PRODUTO_NAO_ENCONTRADO");

            if (!produto.Ativo)
                return Result.Fail<VendaDto>($"Produto '{produto.Nome}' está inativo.", "PRODUTO_INATIVO");

            // Determinar quantidade em unidade base e nome da embalagem
            string? embNome = null;
            int? unPorEmb = null;
            int qtdUnidadeBase = itemReq.Quantidade;

            if (itemReq.EmbalagemId.HasValue)
            {
                var emb = produto.Embalagens.FirstOrDefault(e => e.Id == itemReq.EmbalagemId.Value);
                if (emb != null)
                {
                    embNome = emb.Nome;
                    unPorEmb = emb.QuantidadeUnidades;
                    qtdUnidadeBase = itemReq.Quantidade * emb.QuantidadeUnidades;
                }
            }

            // RN-004: Validar estoque disponível
            var estoque = await _estoqueRepo.ObterAsync(itemReq.ProdutoId, req.FilialId, ct);
            if (estoque is null || qtdUnidadeBase > estoque.EstoqueDisponivel)
            {
                var disp = estoque?.EstoqueDisponivel ?? 0;
                return Result.Fail<VendaDto>(
                    $"Estoque insuficiente para '{produto.Nome}'. Disponível: {disp}, Solicitado: {qtdUnidadeBase}",
                    "ESTOQUE_INSUFICIENTE");
            }

            venda.AdicionarItem(itemReq.ProdutoId, produto.Nome, itemReq.Quantidade,
                itemReq.PrecoUnitario, embNome, unPorEmb);
        }

        // Aplicar desconto excepcional (RN-019: sem automático)
        if (req.Desconto > 0)
            venda.AplicarDesconto(req.Desconto, cmd.UsuarioId);

        // Adicionar pagamentos
        if (req.Pagamentos == null || req.Pagamentos.Count == 0)
            return Result.Fail<VendaDto>("Venda deve ter pelo menos um pagamento.", "SEM_PAGAMENTO");

        foreach (var pgto in req.Pagamentos)
        {
            // Taxas de cartão (configuráveis — valores padrão por enquanto)
            var taxa = pgto.Forma switch
            {
                FormaPagamento.CartaoCredito => 2.5m,
                FormaPagamento.CartaoDebito => 1.5m,
                _ => 0m
            };
            venda.AdicionarPagamento(pgto.Forma, pgto.Valor, taxa);
        }

        // Finalizar venda
        venda.Finalizar();

        // BAIXAR ESTOQUE COM FEFO (RN-009)
        foreach (var item in venda.Itens)
        {
            var qtdBase = item.QuantidadeUnidadeBase;
            var estoque = await _estoqueRepo.ObterAsync(item.ProdutoId, req.FilialId, ct);

            // Buscar lotes FEFO
            var lotes = await _loteRepo.ObterDisponiveisFEFOAsync(item.ProdutoId, req.FilialId, ct);
            var restante = qtdBase;

            foreach (var lote in lotes)
            {
                if (restante <= 0) break;
                var consumir = Math.Min(restante, lote.QuantidadeAtual);
                lote.Consumir(consumir);

                await _movRepo.AdicionarAsync(MovimentacaoEstoque.Criar(
                    item.ProdutoId, req.FilialId, TipoMovimentacao.Venda,
                    -consumir, estoque!.EstoqueFisico, estoque.EstoqueFisico - consumir,
                    cmd.UsuarioId, lote.Id, documentoOrigem: $"Venda #{numero}"), ct);

                restante -= consumir;
            }

            // Se não tem lotes (produto sem controle de lote)
            if (restante > 0 && lotes.Count == 0)
            {
                await _movRepo.AdicionarAsync(MovimentacaoEstoque.Criar(
                    item.ProdutoId, req.FilialId, TipoMovimentacao.Venda,
                    -qtdBase, estoque!.EstoqueFisico, estoque.EstoqueFisico - qtdBase,
                    cmd.UsuarioId, documentoOrigem: $"Venda #{numero}"), ct);
            }

            estoque!.Saida(qtdBase);
        }

        await _vendaRepo.AdicionarAsync(venda, ct);
        await _uow.SaveChangesAsync(ct);

        // Montar DTO de retorno
        var dto = MapToDto(venda);
        return Result.Ok(dto);
    }

    private static VendaDto MapToDto(Venda v) => new(
        v.Id, v.Numero, v.FilialId, v.ClienteId, v.Cliente?.Nome,
        v.Status, v.SubTotal, v.Desconto, v.Total, v.TotalPago, v.Troco, v.TotalItens,
        v.Usuario?.Nome ?? "", v.MotivoCancelamento, v.CriadoEm, v.FinalizadoEm,
        v.Itens.Select(i => new ItemVendaDto(
            i.Id, i.ProdutoId, i.ProdutoNome, i.Quantidade,
            i.PrecoUnitario, i.Total, i.EmbalagemNome, i.UnidadesPorEmbalagem)).ToList(),
        v.Pagamentos.Select(p => new PagamentoVendaDto(
            p.Id, p.Forma, p.Valor, p.TaxaPercentual, p.TaxaValor, p.ValorLiquido)).ToList()
    );
}

// ═══════════════════════════════════════════════════
// CANCELAR VENDA
// ═══════════════════════════════════════════════════
public record CancelarVendaCommand(Guid VendaId, string Motivo, Guid UsuarioId) : IRequest<Result>;

public class CancelarVendaCommandHandler : IRequestHandler<CancelarVendaCommand, Result>
{
    private readonly IVendaRepository _vendaRepo;
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;
    private readonly IUnitOfWork _uow;

    public CancelarVendaCommandHandler(IVendaRepository vendaRepo,
        IEstoqueProdutoRepository estoqueRepo, IMovimentacaoEstoqueRepository movRepo, IUnitOfWork uow)
    {
        _vendaRepo = vendaRepo;
        _estoqueRepo = estoqueRepo;
        _movRepo = movRepo;
        _uow = uow;
    }

    public async Task<Result> Handle(CancelarVendaCommand cmd, CancellationToken ct)
    {
        var venda = await _vendaRepo.ObterComDetalhesAsync(cmd.VendaId, ct);
        if (venda is null)
            return Result.Fail("Venda não encontrada.", "VENDA_NAO_ENCONTRADA");

        venda.Cancelar(cmd.Motivo);

        // Estornar estoque se venda estava finalizada
        if (venda.FinalizadoEm.HasValue)
        {
            foreach (var item in venda.Itens)
            {
                var qtdBase = item.QuantidadeUnidadeBase;
                var estoque = await _estoqueRepo.ObterAsync(item.ProdutoId, venda.FilialId, ct);
                if (estoque != null)
                {
                    var saldoAnterior = estoque.EstoqueFisico;
                    estoque.Entrada(qtdBase);
                    await _movRepo.AdicionarAsync(MovimentacaoEstoque.Criar(
                        item.ProdutoId, venda.FilialId, TipoMovimentacao.Devolucao,
                        qtdBase, saldoAnterior, estoque.EstoqueFisico,
                        cmd.UsuarioId, motivo: $"Cancelamento venda #{venda.Numero}: {cmd.Motivo}"), ct);
                }
            }
        }

        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
