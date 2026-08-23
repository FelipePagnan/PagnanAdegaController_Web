using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Commands.Financeiro;

// === ABRIR CAIXA ===
public record AbrirCaixaCommand(Guid FilialId, decimal SaldoAbertura, Guid UsuarioId) : IRequest<Result<CaixaDto>>;

public class AbrirCaixaHandler : IRequestHandler<AbrirCaixaCommand, Result<CaixaDto>>
{
    private readonly ICaixaRepository _repo;
    private readonly IUnitOfWork _uow;

    public AbrirCaixaHandler(ICaixaRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<Result<CaixaDto>> Handle(AbrirCaixaCommand cmd, CancellationToken ct)
    {
        var existente = await _repo.ObterAbertoAsync(cmd.FilialId, ct);
        if (existente != null)
            return Result.Fail<CaixaDto>("Já existe um caixa aberto nesta filial.", "CAIXA_JA_ABERTO");

        var numero = await _repo.ProximoNumeroAsync(cmd.FilialId, ct);
        var caixa = Caixa.Abrir(cmd.FilialId, numero, cmd.UsuarioId, cmd.SaldoAbertura);

        await _repo.AdicionarAsync(caixa, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Ok(new CaixaDto(
            caixa.Id, caixa.Numero, caixa.FilialId, "", caixa.Status,
            caixa.SaldoAbertura, caixa.TotalEntradas, caixa.TotalSaidas,
            caixa.SaldoAtual, caixa.SaldoFechamento, null, caixa.CriadoEm, null));
    }
}

// === FECHAR CAIXA ===
public record FecharCaixaCommand(Guid FilialId, string? Observacao, Guid UsuarioId) : IRequest<Result<CaixaDto>>;

public class FecharCaixaHandler : IRequestHandler<FecharCaixaCommand, Result<CaixaDto>>
{
    private readonly ICaixaRepository _repo;
    private readonly IUnitOfWork _uow;

    public FecharCaixaHandler(ICaixaRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<Result<CaixaDto>> Handle(FecharCaixaCommand cmd, CancellationToken ct)
    {
        var caixa = await _repo.ObterAbertoAsync(cmd.FilialId, ct);
        if (caixa is null)
            return Result.Fail<CaixaDto>("Nenhum caixa aberto nesta filial.", "CAIXA_NAO_ENCONTRADO");

        caixa.Fechar(cmd.Observacao);
        await _uow.SaveChangesAsync(ct);

        return Result.Ok(new CaixaDto(
            caixa.Id, caixa.Numero, caixa.FilialId, caixa.Usuario?.Nome ?? "", caixa.Status,
            caixa.SaldoAbertura, caixa.TotalEntradas, caixa.TotalSaidas,
            caixa.SaldoAtual, caixa.SaldoFechamento, caixa.ObservacaoFechamento,
            caixa.CriadoEm, caixa.FechadoEm));
    }
}

// === CRIAR CONTA A PAGAR ===
public record CriarContaPagarCommand(CriarContaPagarRequest Request) : IRequest<Result<Guid>>;

public class CriarContaPagarHandler : IRequestHandler<CriarContaPagarCommand, Result<Guid>>
{
    private readonly IContaPagarRepository _repo;
    private readonly IUnitOfWork _uow;

    public CriarContaPagarHandler(IContaPagarRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<Result<Guid>> Handle(CriarContaPagarCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        var conta = ContaPagar.Criar(r.FilialId, r.Descricao, r.Valor, r.DataVencimento,
            r.FornecedorId, observacoes: r.Observacoes);
        await _repo.AdicionarAsync(conta, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Ok(conta.Id);
    }
}

// === PAGAR CONTA ===
public record PagarContaCommand(Guid ContaId, decimal ValorPago, Guid UsuarioId) : IRequest<Result>;

public class PagarContaHandler : IRequestHandler<PagarContaCommand, Result>
{
    private readonly IContaPagarRepository _repo;
    private readonly IUnitOfWork _uow;

    public PagarContaHandler(IContaPagarRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<Result> Handle(PagarContaCommand cmd, CancellationToken ct)
    {
        var conta = await _repo.ObterPorIdAsync(cmd.ContaId, ct);
        if (conta is null) return Result.Fail("Conta não encontrada.", "CONTA_NAO_ENCONTRADA");

        conta.Pagar(cmd.ValorPago, cmd.UsuarioId);
        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

// === CRIAR CONTA A RECEBER ===
public record CriarContaReceberCommand(CriarContaReceberRequest Request) : IRequest<Result<Guid>>;

public class CriarContaReceberHandler : IRequestHandler<CriarContaReceberCommand, Result<Guid>>
{
    private readonly IContaReceberRepository _repo;
    private readonly IUnitOfWork _uow;

    public CriarContaReceberHandler(IContaReceberRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<Result<Guid>> Handle(CriarContaReceberCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;
        var conta = ContaReceber.Criar(r.FilialId, r.Descricao, r.Valor, r.DataVencimento,
            clienteId: r.ClienteId, observacoes: r.Observacoes);
        await _repo.AdicionarAsync(conta, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Ok(conta.Id);
    }
}

// === RECEBER CONTA ===
public record ReceberContaCommand(Guid ContaId, decimal ValorRecebido) : IRequest<Result>;

public class ReceberContaHandler : IRequestHandler<ReceberContaCommand, Result>
{
    private readonly IContaReceberRepository _repo;
    private readonly IUnitOfWork _uow;

    public ReceberContaHandler(IContaReceberRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<Result> Handle(ReceberContaCommand cmd, CancellationToken ct)
    {
        var conta = await _repo.ObterPorIdAsync(cmd.ContaId, ct);
        if (conta is null) return Result.Fail("Conta não encontrada.", "CONTA_NAO_ENCONTRADA");

        conta.Receber(cmd.ValorRecebido);
        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
