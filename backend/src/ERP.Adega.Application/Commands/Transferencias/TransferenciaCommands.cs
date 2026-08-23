using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Commands.Transferencias;

public record CriarTransferenciaCommand(CriarTransferenciaRequest Request, Guid UsuarioId) : IRequest<Result<Guid>>;

public class CriarTransferenciaHandler : IRequestHandler<CriarTransferenciaCommand, Result<Guid>>
{
    private readonly ITransferenciaRepository _repo;
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly IUnitOfWork _uow;

    public CriarTransferenciaHandler(ITransferenciaRepository repo, IEstoqueProdutoRepository estoqueRepo, IUnitOfWork uow)
    { _repo = repo; _estoqueRepo = estoqueRepo; _uow = uow; }

    public async Task<Result<Guid>> Handle(CriarTransferenciaCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        if (req.Itens == null || req.Itens.Count == 0)
            return Result.Fail<Guid>("Transferência deve ter pelo menos um item.", "VAZIA");

        var numero = await _repo.ProximoNumeroAsync(ct);
        var transf = Transferencia.Criar(numero, req.FilialOrigemId, req.FilialDestinoId, cmd.UsuarioId, req.Observacoes);

        foreach (var item in req.Itens)
        {
            var estoque = await _estoqueRepo.ObterAsync(item.ProdutoId, req.FilialOrigemId, ct);
            if (estoque is null || item.Quantidade > estoque.EstoqueDisponivel)
                return Result.Fail<Guid>($"Estoque insuficiente para '{item.ProdutoNome}' na origem.", "ESTOQUE_INSUFICIENTE");

            transf.AdicionarItem(item.ProdutoId, item.ProdutoNome, item.Quantidade);
        }

        await _repo.AdicionarAsync(transf, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Ok(transf.Id);
    }
}

public record AprovarTransferenciaCommand(Guid Id, Guid UsuarioId) : IRequest<Result>;
public class AprovarTransferenciaHandler : IRequestHandler<AprovarTransferenciaCommand, Result>
{
    private readonly ITransferenciaRepository _repo; private readonly IUnitOfWork _uow;
    public AprovarTransferenciaHandler(ITransferenciaRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(AprovarTransferenciaCommand cmd, CancellationToken ct)
    {
        var t = await _repo.ObterPorIdAsync(cmd.Id, ct);
        if (t is null) return Result.Fail("Não encontrada."); t.Aprovar(cmd.UsuarioId);
        await _uow.SaveChangesAsync(ct); return Result.Ok();
    }
}

public record SepararTransferenciaCommand(Guid Id) : IRequest<Result>;
public class SepararTransferenciaHandler : IRequestHandler<SepararTransferenciaCommand, Result>
{
    private readonly ITransferenciaRepository _repo; private readonly IUnitOfWork _uow;
    public SepararTransferenciaHandler(ITransferenciaRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(SepararTransferenciaCommand cmd, CancellationToken ct)
    {
        var t = await _repo.ObterPorIdAsync(cmd.Id, ct);
        if (t is null) return Result.Fail("Não encontrada."); t.MarcarSeparada();
        await _uow.SaveChangesAsync(ct); return Result.Ok();
    }
}

// Enviar: baixa estoque da origem
public record EnviarTransferenciaCommand(Guid Id, Guid UsuarioId) : IRequest<Result>;
public class EnviarTransferenciaHandler : IRequestHandler<EnviarTransferenciaCommand, Result>
{
    private readonly ITransferenciaRepository _repo; private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly IMovimentacaoEstoqueRepository _movRepo; private readonly IUnitOfWork _uow;
    public EnviarTransferenciaHandler(ITransferenciaRepository repo, IEstoqueProdutoRepository estoqueRepo,
        IMovimentacaoEstoqueRepository movRepo, IUnitOfWork uow) { _repo = repo; _estoqueRepo = estoqueRepo; _movRepo = movRepo; _uow = uow; }

    public async Task<Result> Handle(EnviarTransferenciaCommand cmd, CancellationToken ct)
    {
        var t = await _repo.ObterComDetalhesAsync(cmd.Id, ct);
        if (t is null) return Result.Fail("Não encontrada.");

        t.MarcarEnviada();

        foreach (var item in t.Itens)
        {
            var estoque = await _estoqueRepo.ObterAsync(item.ProdutoId, t.FilialOrigemId, ct);
            if (estoque is null) continue;
            var anterior = estoque.EstoqueFisico;
            estoque.Saida(item.Quantidade);
            await _movRepo.AdicionarAsync(MovimentacaoEstoque.Criar(
                item.ProdutoId, t.FilialOrigemId, TipoMovimentacao.Transferencia,
                -item.Quantidade, anterior, estoque.EstoqueFisico, cmd.UsuarioId,
                documentoOrigem: $"Transf #{t.Numero} → destino"), ct);
        }

        await _uow.SaveChangesAsync(ct); return Result.Ok();
    }
}

// Receber: entrada no estoque destino
public record ReceberTransferenciaCommand(Guid Id, Guid UsuarioId) : IRequest<Result>;
public class ReceberTransferenciaHandler : IRequestHandler<ReceberTransferenciaCommand, Result>
{
    private readonly ITransferenciaRepository _repo; private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly IMovimentacaoEstoqueRepository _movRepo; private readonly IUnitOfWork _uow;
    public ReceberTransferenciaHandler(ITransferenciaRepository repo, IEstoqueProdutoRepository estoqueRepo,
        IMovimentacaoEstoqueRepository movRepo, IUnitOfWork uow) { _repo = repo; _estoqueRepo = estoqueRepo; _movRepo = movRepo; _uow = uow; }

    public async Task<Result> Handle(ReceberTransferenciaCommand cmd, CancellationToken ct)
    {
        var t = await _repo.ObterComDetalhesAsync(cmd.Id, ct);
        if (t is null) return Result.Fail("Não encontrada.");

        t.RegistrarRecebimento();

        foreach (var item in t.Itens)
        {
            var estoque = await _estoqueRepo.ObterAsync(item.ProdutoId, t.FilialDestinoId, ct);
            if (estoque is null)
            {
                estoque = EstoqueProduto.Criar(item.ProdutoId, t.FilialDestinoId);
                await _estoqueRepo.AdicionarAsync(estoque, ct);
            }
            var anterior = estoque.EstoqueFisico;
            estoque.Entrada(item.Quantidade);
            await _movRepo.AdicionarAsync(MovimentacaoEstoque.Criar(
                item.ProdutoId, t.FilialDestinoId, TipoMovimentacao.Transferencia,
                item.Quantidade, anterior, estoque.EstoqueFisico, cmd.UsuarioId,
                documentoOrigem: $"Transf #{t.Numero} ← origem"), ct);
        }

        await _uow.SaveChangesAsync(ct); return Result.Ok();
    }
}

public record CancelarTransferenciaCommand(Guid Id, string Motivo) : IRequest<Result>;
public class CancelarTransferenciaHandler : IRequestHandler<CancelarTransferenciaCommand, Result>
{
    private readonly ITransferenciaRepository _repo; private readonly IUnitOfWork _uow;
    public CancelarTransferenciaHandler(ITransferenciaRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task<Result> Handle(CancelarTransferenciaCommand cmd, CancellationToken ct)
    {
        var t = await _repo.ObterPorIdAsync(cmd.Id, ct);
        if (t is null) return Result.Fail("Não encontrada."); t.Cancelar(cmd.Motivo);
        await _uow.SaveChangesAsync(ct); return Result.Ok();
    }
}

// === QUERIES ===
namespace ERP.Adega.Application.Queries.Transferencias;

public record ListarTransferenciasQuery(Guid? FilialId, StatusTransferencia? Status, int Pagina = 1, int TamanhoPagina = 20)
    : IRequest<PagedResult<TransferenciaResumoDto>>;

public class ListarTransferenciasHandler : IRequestHandler<ListarTransferenciasQuery, PagedResult<TransferenciaResumoDto>>
{
    private readonly ITransferenciaRepository _repo;
    public ListarTransferenciasHandler(ITransferenciaRepository repo) => _repo = repo;
    public async Task<PagedResult<TransferenciaResumoDto>> Handle(ListarTransferenciasQuery q, CancellationToken ct)
    {
        var total = await _repo.ContarAsync(q.FilialId, q.Status, ct);
        var items = await _repo.ListarAsync(q.FilialId, q.Status, q.Pagina, q.TamanhoPagina, ct);
        var dtos = items.Select(t => new TransferenciaResumoDto(t.Id, t.Numero,
            t.FilialOrigem?.Nome ?? "", t.FilialDestino?.Nome ?? "", t.Status, t.TotalItens,
            t.Solicitante?.Nome ?? "", t.CriadoEm)).ToList();
        return new PagedResult<TransferenciaResumoDto>(dtos, total, q.Pagina, q.TamanhoPagina);
    }
}

public record ObterTransferenciaQuery(Guid Id) : IRequest<Result<TransferenciaDto>>;
public class ObterTransferenciaHandler : IRequestHandler<ObterTransferenciaQuery, Result<TransferenciaDto>>
{
    private readonly ITransferenciaRepository _repo;
    public ObterTransferenciaHandler(ITransferenciaRepository repo) => _repo = repo;
    public async Task<Result<TransferenciaDto>> Handle(ObterTransferenciaQuery q, CancellationToken ct)
    {
        var t = await _repo.ObterComDetalhesAsync(q.Id, ct);
        if (t is null) return Result.Fail<TransferenciaDto>("Não encontrada.");
        return Result.Ok(new TransferenciaDto(t.Id, t.Numero, t.FilialOrigem?.Nome ?? "", t.FilialDestino?.Nome ?? "",
            t.Status, t.TotalItens, t.Solicitante?.Nome ?? "", t.Observacoes, t.AprovadoEm, t.EnviadoEm, t.RecebidoEm,
            t.MotivoCancelamento, t.CriadoEm,
            t.Itens.Select(i => new ItemTransferenciaDto(i.Id, i.ProdutoId, i.ProdutoNome, i.Quantidade, i.QuantidadeRecebida)).ToList()));
    }
}
