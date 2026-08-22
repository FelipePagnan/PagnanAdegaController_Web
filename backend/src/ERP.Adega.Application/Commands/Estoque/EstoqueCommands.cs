using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Commands.Estoque;

// ═══════════════════════════════════════════════════
// ENTRADA DE ESTOQUE (compra, devolução)
// ═══════════════════════════════════════════════════
public record EntradaEstoqueCommand(
    Guid ProdutoId,
    Guid FilialId,
    int Quantidade,
    decimal CustoUnitario,
    string? CodigoLote,
    DateTime? DataValidade,
    DateTime? DataFabricacao,
    Guid? FornecedorId,
    string? NotaFiscal,
    Guid UsuarioId
) : IRequest<Result<Guid>>;

public class EntradaEstoqueCommandHandler : IRequestHandler<EntradaEstoqueCommand, Result<Guid>>
{
    private readonly IProdutoRepository _produtoRepo;
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly ILoteRepository _loteRepo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;
    private readonly IUnitOfWork _uow;

    public EntradaEstoqueCommandHandler(
        IProdutoRepository produtoRepo, IEstoqueProdutoRepository estoqueRepo,
        ILoteRepository loteRepo, IMovimentacaoEstoqueRepository movRepo, IUnitOfWork uow)
    {
        _produtoRepo = produtoRepo;
        _estoqueRepo = estoqueRepo;
        _loteRepo = loteRepo;
        _movRepo = movRepo;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(EntradaEstoqueCommand cmd, CancellationToken ct)
    {
        var produto = await _produtoRepo.ObterPorIdAsync(cmd.ProdutoId, ct);
        if (produto is null)
            return Result.Fail<Guid>("Produto não encontrado.", "PRODUTO_NAO_ENCONTRADO");

        if (!produto.Ativo)
            return Result.Fail<Guid>("Produto inativo.", "PRODUTO_INATIVO");

        // Validar validade obrigatória se produto controla validade
        if (produto.ControlaValidade && !cmd.DataValidade.HasValue)
            return Result.Fail<Guid>("Data de validade é obrigatória para este produto.", "VALIDADE_OBRIGATORIA");

        // Criar lote
        var codigoLote = cmd.CodigoLote ?? $"L{DateTime.UtcNow:yyyyMMddHHmmss}";
        var lote = Lote.Criar(cmd.ProdutoId, cmd.FilialId, codigoLote, cmd.Quantidade,
            cmd.CustoUnitario, cmd.DataValidade, cmd.DataFabricacao, cmd.FornecedorId, cmd.NotaFiscal);
        await _loteRepo.AdicionarAsync(lote, ct);

        // Buscar ou criar saldo de estoque
        var estoque = await _estoqueRepo.ObterAsync(cmd.ProdutoId, cmd.FilialId, ct);
        if (estoque is null)
        {
            estoque = EstoqueProduto.Criar(cmd.ProdutoId, cmd.FilialId);
            await _estoqueRepo.AdicionarAsync(estoque, ct);
        }

        var saldoAnterior = estoque.EstoqueFisico;
        estoque.Entrada(cmd.Quantidade);

        // Registrar movimentação (RN-003)
        var mov = MovimentacaoEstoque.Criar(
            cmd.ProdutoId, cmd.FilialId, TipoMovimentacao.Entrada,
            cmd.Quantidade, saldoAnterior, estoque.EstoqueFisico,
            cmd.UsuarioId, lote.Id, documentoOrigem: cmd.NotaFiscal);
        await _movRepo.AdicionarAsync(mov, ct);

        // Atualizar preço de custo do produto
        produto.AtualizarPrecoCusto(cmd.CustoUnitario);

        await _uow.SaveChangesAsync(ct);
        return Result.Ok(lote.Id);
    }
}

// ═══════════════════════════════════════════════════
// SAÍDA DE ESTOQUE COM FEFO (venda, perda, dano)
// RN-009: Prioriza lote com vencimento mais próximo
// ═══════════════════════════════════════════════════
public record SaidaEstoqueCommand(
    Guid ProdutoId,
    Guid FilialId,
    int Quantidade,
    TipoMovimentacao Tipo,
    Guid UsuarioId,
    string? Motivo,
    string? DocumentoOrigem
) : IRequest<Result>;

public class SaidaEstoqueCommandHandler : IRequestHandler<SaidaEstoqueCommand, Result>
{
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly ILoteRepository _loteRepo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;
    private readonly IUnitOfWork _uow;

    public SaidaEstoqueCommandHandler(
        IEstoqueProdutoRepository estoqueRepo, ILoteRepository loteRepo,
        IMovimentacaoEstoqueRepository movRepo, IUnitOfWork uow)
    {
        _estoqueRepo = estoqueRepo;
        _loteRepo = loteRepo;
        _movRepo = movRepo;
        _uow = uow;
    }

    public async Task<Result> Handle(SaidaEstoqueCommand cmd, CancellationToken ct)
    {
        var estoque = await _estoqueRepo.ObterAsync(cmd.ProdutoId, cmd.FilialId, ct);
        if (estoque is null)
            return Result.Fail("Produto não possui estoque nesta filial.", "ESTOQUE_NAO_ENCONTRADO");

        // RN-001: Validar disponibilidade
        if (cmd.Quantidade > estoque.EstoqueDisponivel)
            return Result.Fail(
                $"Estoque insuficiente. Disponível: {estoque.EstoqueDisponivel}, Solicitado: {cmd.Quantidade}",
                "ESTOQUE_INSUFICIENTE");

        // RN-009: Buscar lotes por FEFO (vencimento mais próximo primeiro)
        var lotes = await _loteRepo.ObterDisponiveisFEFOAsync(cmd.ProdutoId, cmd.FilialId, ct);

        var restante = cmd.Quantidade;
        var movimentacoes = new List<MovimentacaoEstoque>();

        foreach (var lote in lotes)
        {
            if (restante <= 0) break;

            var consumir = Math.Min(restante, lote.QuantidadeAtual);
            lote.Consumir(consumir);

            movimentacoes.Add(MovimentacaoEstoque.Criar(
                cmd.ProdutoId, cmd.FilialId, cmd.Tipo,
                -consumir, estoque.EstoqueFisico, estoque.EstoqueFisico - consumir,
                cmd.UsuarioId, lote.Id, cmd.Motivo, cmd.DocumentoOrigem));

            restante -= consumir;
        }

        // Baixar estoque físico
        var saldoAnterior = estoque.EstoqueFisico;
        estoque.Saida(cmd.Quantidade);

        // Se não conseguiu alocar em lotes (produto sem lote), registra movimentação geral
        if (movimentacoes.Count == 0)
        {
            movimentacoes.Add(MovimentacaoEstoque.Criar(
                cmd.ProdutoId, cmd.FilialId, cmd.Tipo,
                -cmd.Quantidade, saldoAnterior, estoque.EstoqueFisico,
                cmd.UsuarioId, motivo: cmd.Motivo, documentoOrigem: cmd.DocumentoOrigem));
        }

        await _movRepo.AdicionarVariasAsync(movimentacoes, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}

// ═══════════════════════════════════════════════════
// AJUSTE DE INVENTÁRIO (RN-010, RN-012)
// ═══════════════════════════════════════════════════
public record AjusteEstoqueCommand(
    Guid ProdutoId,
    Guid FilialId,
    int NovaQuantidade,
    string Motivo,
    Guid UsuarioId
) : IRequest<Result>;

public class AjusteEstoqueCommandHandler : IRequestHandler<AjusteEstoqueCommand, Result>
{
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;
    private readonly IUnitOfWork _uow;

    public AjusteEstoqueCommandHandler(
        IEstoqueProdutoRepository estoqueRepo, IMovimentacaoEstoqueRepository movRepo, IUnitOfWork uow)
    {
        _estoqueRepo = estoqueRepo;
        _movRepo = movRepo;
        _uow = uow;
    }

    public async Task<Result> Handle(AjusteEstoqueCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.Motivo))
            return Result.Fail("Motivo é obrigatório para ajuste de estoque.", "MOTIVO_OBRIGATORIO");

        var estoque = await _estoqueRepo.ObterAsync(cmd.ProdutoId, cmd.FilialId, ct);
        if (estoque is null)
            return Result.Fail("Produto não possui estoque nesta filial.", "ESTOQUE_NAO_ENCONTRADO");

        var saldoAnterior = estoque.EstoqueFisico;
        var diferenca = cmd.NovaQuantidade - saldoAnterior;

        if (diferenca == 0)
            return Result.Fail("Nova quantidade é igual ao saldo atual.", "SEM_DIFERENCA");

        estoque.AjustarInventario(cmd.NovaQuantidade);

        var mov = MovimentacaoEstoque.Criar(
            cmd.ProdutoId, cmd.FilialId, TipoMovimentacao.Ajuste,
            diferenca, saldoAnterior, cmd.NovaQuantidade,
            cmd.UsuarioId, motivo: cmd.Motivo);
        await _movRepo.AdicionarAsync(mov, ct);

        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
