using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Queries.Estoque;

// === Estoque de uma filial ===
public record ListarEstoqueQuery(
    Guid FilialId,
    string? Termo,
    NivelAlertaEstoque? NivelAlerta,
    int Pagina = 1,
    int TamanhoPagina = 20
) : IRequest<PagedResult<EstoqueProdutoDto>>;

public class ListarEstoqueQueryHandler : IRequestHandler<ListarEstoqueQuery, PagedResult<EstoqueProdutoDto>>
{
    private readonly IEstoqueProdutoRepository _estoqueRepo;

    public ListarEstoqueQueryHandler(IEstoqueProdutoRepository estoqueRepo) => _estoqueRepo = estoqueRepo;

    public async Task<PagedResult<EstoqueProdutoDto>> Handle(ListarEstoqueQuery query, CancellationToken ct)
    {
        var todos = await _estoqueRepo.ObterPorFilialAsync(query.FilialId, ct);

        var filtrado = todos.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Termo))
            filtrado = filtrado.Where(e => e.Produto.Nome.Contains(query.Termo, StringComparison.OrdinalIgnoreCase));

        var lista = filtrado.Select(e =>
        {
            var embalagem = e.Produto.Embalagens.FirstOrDefault();
            var (fardos, unRest) = embalagem != null
                ? e.CalcularFardosUnidades(embalagem.QuantidadeUnidades)
                : (0, e.EstoqueFisico);

            return new EstoqueProdutoDto(
                e.Id, e.ProdutoId, e.Produto.Nome, e.FilialId,
                e.EstoqueFisico, e.EstoqueReservado, e.EstoqueDisponivel,
                e.LocalizacaoFisica,
                e.Produto.CalcularAlerta(e.EstoqueDisponivel),
                embalagem?.QuantidadeUnidades,
                embalagem != null ? fardos : null,
                embalagem != null ? unRest : null,
                e.AtualizadoEm ?? e.CriadoEm
            );
        }).ToList();

        if (query.NivelAlerta.HasValue)
            lista = lista.Where(e => e.NivelAlerta == query.NivelAlerta.Value).ToList();

        var total = lista.Count;
        var paged = lista
            .Skip((query.Pagina - 1) * query.TamanhoPagina)
            .Take(query.TamanhoPagina)
            .ToList();

        return new PagedResult<EstoqueProdutoDto>(paged, total, query.Pagina, query.TamanhoPagina);
    }
}

// === Saldo específico de um produto ===
public record ObterSaldoQuery(Guid ProdutoId, Guid FilialId) : IRequest<Result<EstoqueProdutoDto>>;

public class ObterSaldoQueryHandler : IRequestHandler<ObterSaldoQuery, Result<EstoqueProdutoDto>>
{
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly IProdutoRepository _produtoRepo;

    public ObterSaldoQueryHandler(IEstoqueProdutoRepository estoqueRepo, IProdutoRepository produtoRepo)
    {
        _estoqueRepo = estoqueRepo;
        _produtoRepo = produtoRepo;
    }

    public async Task<Result<EstoqueProdutoDto>> Handle(ObterSaldoQuery query, CancellationToken ct)
    {
        var estoque = await _estoqueRepo.ObterAsync(query.ProdutoId, query.FilialId, ct);
        if (estoque is null)
            return Result.Fail<EstoqueProdutoDto>("Estoque não encontrado.", "ESTOQUE_NAO_ENCONTRADO");

        var produto = await _produtoRepo.ObterComDetalhesAsync(query.ProdutoId, ct);
        var embalagem = produto?.Embalagens.FirstOrDefault();
        var (fardos, unRest) = embalagem != null
            ? estoque.CalcularFardosUnidades(embalagem.QuantidadeUnidades)
            : (0, estoque.EstoqueFisico);

        var dto = new EstoqueProdutoDto(
            estoque.Id, estoque.ProdutoId, produto?.Nome ?? "", estoque.FilialId,
            estoque.EstoqueFisico, estoque.EstoqueReservado, estoque.EstoqueDisponivel,
            estoque.LocalizacaoFisica,
            produto?.CalcularAlerta(estoque.EstoqueDisponivel) ?? NivelAlertaEstoque.Normal,
            embalagem?.QuantidadeUnidades, embalagem != null ? fardos : null,
            embalagem != null ? unRest : null, estoque.AtualizadoEm ?? estoque.CriadoEm);

        return Result.Ok(dto);
    }
}

// === Alertas de estoque ===
public record ListarAlertasQuery(Guid FilialId) : IRequest<IReadOnlyList<EstoqueProdutoDto>>;

public class ListarAlertasQueryHandler : IRequestHandler<ListarAlertasQuery, IReadOnlyList<EstoqueProdutoDto>>
{
    private readonly IEstoqueProdutoRepository _estoqueRepo;

    public ListarAlertasQueryHandler(IEstoqueProdutoRepository estoqueRepo) => _estoqueRepo = estoqueRepo;

    public async Task<IReadOnlyList<EstoqueProdutoDto>> Handle(ListarAlertasQuery query, CancellationToken ct)
    {
        var todos = await _estoqueRepo.ObterPorFilialAsync(query.FilialId, ct);

        return todos
            .Where(e => e.Produto.CalcularAlerta(e.EstoqueDisponivel) != NivelAlertaEstoque.Normal)
            .Select(e => new EstoqueProdutoDto(
                e.Id, e.ProdutoId, e.Produto.Nome, e.FilialId,
                e.EstoqueFisico, e.EstoqueReservado, e.EstoqueDisponivel,
                e.LocalizacaoFisica,
                e.Produto.CalcularAlerta(e.EstoqueDisponivel),
                null, null, null, e.AtualizadoEm ?? e.CriadoEm))
            .ToList();
    }
}

// === Histórico de movimentações ===
public record ListarMovimentacoesQuery(
    Guid ProdutoId,
    Guid FilialId,
    DateTime? Inicio,
    DateTime? Fim,
    int Pagina = 1,
    int TamanhoPagina = 50
) : IRequest<PagedResult<MovimentacaoEstoqueDto>>;

public class ListarMovimentacoesQueryHandler : IRequestHandler<ListarMovimentacoesQuery, PagedResult<MovimentacaoEstoqueDto>>
{
    private readonly IMovimentacaoEstoqueRepository _movRepo;

    public ListarMovimentacoesQueryHandler(IMovimentacaoEstoqueRepository movRepo) => _movRepo = movRepo;

    public async Task<PagedResult<MovimentacaoEstoqueDto>> Handle(ListarMovimentacoesQuery query, CancellationToken ct)
    {
        var movs = await _movRepo.ObterPorProdutoAsync(
            query.ProdutoId, query.FilialId, query.Inicio, query.Fim,
            query.Pagina, query.TamanhoPagina, ct);

        var dtos = movs.Select(m => new MovimentacaoEstoqueDto(
            m.Id, m.ProdutoId, m.Produto?.Nome ?? "", m.Tipo,
            m.Quantidade, m.SaldoAnterior, m.SaldoPosterior,
            m.Lote?.Codigo, m.Motivo, m.DocumentoOrigem,
            m.Usuario?.Nome ?? "", m.CriadoEm
        )).ToList();

        return new PagedResult<MovimentacaoEstoqueDto>(dtos, dtos.Count, query.Pagina, query.TamanhoPagina);
    }
}

// === Lotes vencendo ===
public record ListarLotesVencendoQuery(Guid FilialId, int Dias = 30) : IRequest<IReadOnlyList<LoteDto>>;

public class ListarLotesVencendoQueryHandler : IRequestHandler<ListarLotesVencendoQuery, IReadOnlyList<LoteDto>>
{
    private readonly ILoteRepository _loteRepo;

    public ListarLotesVencendoQueryHandler(ILoteRepository loteRepo) => _loteRepo = loteRepo;

    public async Task<IReadOnlyList<LoteDto>> Handle(ListarLotesVencendoQuery query, CancellationToken ct)
    {
        var lotes = await _loteRepo.ObterVencendoAsync(query.FilialId, query.Dias, ct);

        return lotes.Select(l => new LoteDto(
            l.Id, l.ProdutoId, l.Codigo, l.DataFabricacao, l.DataValidade,
            l.Fornecedor?.RazaoSocial, l.NotaFiscal, l.CustoUnitario,
            l.QuantidadeRecebida, l.QuantidadeAtual,
            l.EstaVencido(), l.EstaVencendo(), l.CriadoEm
        )).ToList();
    }
}
