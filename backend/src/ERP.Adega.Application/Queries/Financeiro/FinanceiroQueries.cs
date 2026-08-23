using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Queries.Financeiro;

// === CAIXA ATUAL ===
public record ObterCaixaAtualQuery(Guid FilialId) : IRequest<Result<CaixaDto>>;

public class ObterCaixaAtualHandler : IRequestHandler<ObterCaixaAtualQuery, Result<CaixaDto>>
{
    private readonly ICaixaRepository _repo;
    public ObterCaixaAtualHandler(ICaixaRepository repo) => _repo = repo;

    public async Task<Result<CaixaDto>> Handle(ObterCaixaAtualQuery query, CancellationToken ct)
    {
        var caixa = await _repo.ObterAbertoAsync(query.FilialId, ct);
        if (caixa is null)
            return Result.Fail<CaixaDto>("Nenhum caixa aberto.", "CAIXA_NAO_ENCONTRADO");

        return Result.Ok(new CaixaDto(
            caixa.Id, caixa.Numero, caixa.FilialId, caixa.Usuario?.Nome ?? "", caixa.Status,
            caixa.SaldoAbertura, caixa.TotalEntradas, caixa.TotalSaidas,
            caixa.SaldoAtual, caixa.SaldoFechamento, null, caixa.CriadoEm, null));
    }
}

// === CONTAS A PAGAR ===
public record ListarContasPagarQuery(Guid FilialId, StatusConta? Status, int Pagina = 1, int TamanhoPagina = 20)
    : IRequest<PagedResult<ContaPagarDto>>;

public class ListarContasPagarHandler : IRequestHandler<ListarContasPagarQuery, PagedResult<ContaPagarDto>>
{
    private readonly IContaPagarRepository _repo;
    public ListarContasPagarHandler(IContaPagarRepository repo) => _repo = repo;

    public async Task<PagedResult<ContaPagarDto>> Handle(ListarContasPagarQuery query, CancellationToken ct)
    {
        var total = await _repo.ContarAsync(query.FilialId, query.Status, ct);
        var contas = await _repo.ListarAsync(query.FilialId, query.Status, query.Pagina, query.TamanhoPagina, ct);

        var dtos = contas.Select(c => new ContaPagarDto(
            c.Id, c.FilialId, c.Fornecedor?.RazaoSocial, c.Descricao,
            c.ValorOriginal, c.ValorPago, c.DataVencimento, c.DataPagamento,
            c.Status, c.Parcela, c.TotalParcelas, c.Observacoes, c.EstaVencida(), c.CriadoEm
        )).ToList();

        return new PagedResult<ContaPagarDto>(dtos, total, query.Pagina, query.TamanhoPagina);
    }
}

// === CONTAS A RECEBER ===
public record ListarContasReceberQuery(Guid FilialId, StatusConta? Status, int Pagina = 1, int TamanhoPagina = 20)
    : IRequest<PagedResult<ContaReceberDto>>;

public class ListarContasReceberHandler : IRequestHandler<ListarContasReceberQuery, PagedResult<ContaReceberDto>>
{
    private readonly IContaReceberRepository _repo;
    public ListarContasReceberHandler(IContaReceberRepository repo) => _repo = repo;

    public async Task<PagedResult<ContaReceberDto>> Handle(ListarContasReceberQuery query, CancellationToken ct)
    {
        var total = await _repo.ContarAsync(query.FilialId, query.Status, ct);
        var contas = await _repo.ListarAsync(query.FilialId, query.Status, query.Pagina, query.TamanhoPagina, ct);

        var dtos = contas.Select(c => new ContaReceberDto(
            c.Id, c.FilialId, c.Cliente?.Nome, c.Descricao,
            c.ValorOriginal, c.ValorRecebido, c.DataVencimento, c.DataRecebimento,
            c.Status, c.FormaPagamento?.ToString(), c.TaxaOperadora, c.ValorLiquido,
            c.Observacoes, c.CriadoEm
        )).ToList();

        return new PagedResult<ContaReceberDto>(dtos, total, query.Pagina, query.TamanhoPagina);
    }
}

// === FLUXO DE CAIXA ===
public record FluxoCaixaQuery(Guid FilialId) : IRequest<FluxoCaixaDto>;

public class FluxoCaixaHandler : IRequestHandler<FluxoCaixaQuery, FluxoCaixaDto>
{
    private readonly IContaPagarRepository _pagarRepo;
    private readonly IContaReceberRepository _receberRepo;

    public FluxoCaixaHandler(IContaPagarRepository pagarRepo, IContaReceberRepository receberRepo)
    { _pagarRepo = pagarRepo; _receberRepo = receberRepo; }

    public async Task<FluxoCaixaDto> Handle(FluxoCaixaQuery query, CancellationToken ct)
    {
        var totalPagar = await _pagarRepo.TotalAbertoAsync(query.FilialId, ct);
        var totalReceber = await _receberRepo.TotalAbertoAsync(query.FilialId, ct);
        var contasPagar = await _pagarRepo.ContarAsync(query.FilialId, StatusConta.Aberta, ct);
        var contasReceber = await _receberRepo.ContarAsync(query.FilialId, StatusConta.Aberta, ct);
        var vencidas = await _pagarRepo.ContarVencidasAsync(query.FilialId, ct);

        // Buscar últimas movimentações para o resumo
        var pagar = await _pagarRepo.ListarAsync(query.FilialId, null, 1, 10, ct);
        var receber = await _receberRepo.ListarAsync(query.FilialId, null, 1, 10, ct);

        var itens = new List<FluxoItemDto>();
        foreach (var p in pagar)
            itens.Add(new FluxoItemDto("Pagar", p.Descricao, p.ValorOriginal, p.DataVencimento, p.Status));
        foreach (var r in receber)
            itens.Add(new FluxoItemDto("Receber", r.Descricao, r.ValorOriginal, r.DataVencimento, r.Status));

        itens = itens.OrderBy(i => i.Data).ToList();

        return new FluxoCaixaDto(totalPagar, totalReceber, totalReceber - totalPagar,
            contasPagar, contasReceber, vencidas, itens);
    }
}
