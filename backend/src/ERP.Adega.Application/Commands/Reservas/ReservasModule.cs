using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Commands.Reservas;

public record CriarReservaCommand(CriarReservaRequest Request, Guid UsuarioId) : IRequest<Result<Guid>>;

public class CriarReservaHandler : IRequestHandler<CriarReservaCommand, Result<Guid>>
{
    private readonly IReservaRepository _reservaRepo;
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly IUnitOfWork _uow;

    public CriarReservaHandler(IReservaRepository reservaRepo, IEstoqueProdutoRepository estoqueRepo, IUnitOfWork uow)
    { _reservaRepo = reservaRepo; _estoqueRepo = estoqueRepo; _uow = uow; }

    public async Task<Result<Guid>> Handle(CriarReservaCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;
        if (req.Itens == null || req.Itens.Count == 0)
            return Result.Fail<Guid>("Reserva deve ter pelo menos um item.", "RESERVA_VAZIA");

        var numero = await _reservaRepo.ProximoNumeroAsync(req.FilialId, ct);
        var reserva = Reserva.Criar(numero, req.ClienteId, req.FilialId, cmd.UsuarioId,
            req.ValorAdiantamento, req.DataLimite, req.Observacoes);

        foreach (var item in req.Itens)
        {
            // Validar estoque disponível
            var estoque = await _estoqueRepo.ObterAsync(item.ProdutoId, req.FilialId, ct);
            if (estoque is null || item.Quantidade > estoque.EstoqueDisponivel)
                return Result.Fail<Guid>($"Estoque insuficiente para '{item.ProdutoNome}'.", "ESTOQUE_INSUFICIENTE");

            reserva.AdicionarItem(item.ProdutoId, item.ProdutoNome, item.Quantidade, item.PrecoUnitario);

            // RN-013: Reserva reduz estoque disponível
            estoque.Reservar(item.Quantidade);
        }

        await _reservaRepo.AdicionarAsync(reserva, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Ok(reserva.Id);
    }
}

public record RetirarReservaCommand(Guid ReservaId, Guid UsuarioId) : IRequest<Result>;

public class RetirarReservaHandler : IRequestHandler<RetirarReservaCommand, Result>
{
    private readonly IReservaRepository _reservaRepo;
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;
    private readonly IUnitOfWork _uow;

    public RetirarReservaHandler(IReservaRepository reservaRepo, IEstoqueProdutoRepository estoqueRepo,
        IMovimentacaoEstoqueRepository movRepo, IUnitOfWork uow)
    { _reservaRepo = reservaRepo; _estoqueRepo = estoqueRepo; _movRepo = movRepo; _uow = uow; }

    public async Task<Result> Handle(RetirarReservaCommand cmd, CancellationToken ct)
    {
        var reserva = await _reservaRepo.ObterComDetalhesAsync(cmd.ReservaId, ct);
        if (reserva is null) return Result.Fail("Reserva não encontrada.", "RESERVA_NAO_ENCONTRADA");

        reserva.Retirar();

        // Liberar reserva e dar baixa no estoque físico
        foreach (var item in reserva.Itens)
        {
            var estoque = await _estoqueRepo.ObterAsync(item.ProdutoId, reserva.FilialId, ct);
            if (estoque is null) continue;

            estoque.LiberarReserva(item.Quantidade);
            var saldoAnterior = estoque.EstoqueFisico;
            estoque.Saida(item.Quantidade);

            await _movRepo.AdicionarAsync(MovimentacaoEstoque.Criar(
                item.ProdutoId, reserva.FilialId, TipoMovimentacao.Venda,
                -item.Quantidade, saldoAnterior, estoque.EstoqueFisico,
                cmd.UsuarioId, documentoOrigem: $"Reserva #{reserva.Numero}"), ct);
        }

        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

public record CancelarReservaCommand(Guid ReservaId, string Motivo) : IRequest<Result>;

public class CancelarReservaHandler : IRequestHandler<CancelarReservaCommand, Result>
{
    private readonly IReservaRepository _reservaRepo;
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly IUnitOfWork _uow;

    public CancelarReservaHandler(IReservaRepository reservaRepo, IEstoqueProdutoRepository estoqueRepo, IUnitOfWork uow)
    { _reservaRepo = reservaRepo; _estoqueRepo = estoqueRepo; _uow = uow; }

    public async Task<Result> Handle(CancelarReservaCommand cmd, CancellationToken ct)
    {
        var reserva = await _reservaRepo.ObterComDetalhesAsync(cmd.ReservaId, ct);
        if (reserva is null) return Result.Fail("Reserva não encontrada.", "RESERVA_NAO_ENCONTRADA");

        reserva.Cancelar(cmd.Motivo);

        // Liberar estoque reservado
        foreach (var item in reserva.Itens)
        {
            var estoque = await _estoqueRepo.ObterAsync(item.ProdutoId, reserva.FilialId, ct);
            estoque?.LiberarReserva(item.Quantidade);
        }

        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

// === QUERIES ===
namespace ERP.Adega.Application.Queries.Reservas;

public record ListarReservasQuery(Guid FilialId, StatusReserva? Status, int Pagina = 1, int TamanhoPagina = 20)
    : IRequest<PagedResult<ReservaResumoDto>>;

public class ListarReservasHandler : IRequestHandler<ListarReservasQuery, PagedResult<ReservaResumoDto>>
{
    private readonly IReservaRepository _repo;
    public ListarReservasHandler(IReservaRepository repo) => _repo = repo;

    public async Task<PagedResult<ReservaResumoDto>> Handle(ListarReservasQuery query, CancellationToken ct)
    {
        var total = await _repo.ContarAsync(query.FilialId, query.Status, ct);
        var reservas = await _repo.ListarAsync(query.FilialId, query.Status, query.Pagina, query.TamanhoPagina, ct);

        var dtos = reservas.Select(r => new ReservaResumoDto(
            r.Id, r.Numero, r.Cliente?.Nome ?? "", r.Status,
            r.ValorTotal, r.DataLimite, r.Itens.Count, r.EstaExpirada(), r.CriadoEm
        )).ToList();

        return new PagedResult<ReservaResumoDto>(dtos, total, query.Pagina, query.TamanhoPagina);
    }
}

public record ObterReservaQuery(Guid Id) : IRequest<Result<ReservaDto>>;

public class ObterReservaHandler : IRequestHandler<ObterReservaQuery, Result<ReservaDto>>
{
    private readonly IReservaRepository _repo;
    public ObterReservaHandler(IReservaRepository repo) => _repo = repo;

    public async Task<Result<ReservaDto>> Handle(ObterReservaQuery query, CancellationToken ct)
    {
        var r = await _repo.ObterComDetalhesAsync(query.Id, ct);
        if (r is null) return Result.Fail<ReservaDto>("Reserva não encontrada.", "RESERVA_NAO_ENCONTRADA");

        return Result.Ok(new ReservaDto(
            r.Id, r.Numero, r.Cliente?.Nome ?? "", r.Status,
            r.ValorTotal, r.ValorAdiantamento, r.ValorRestante,
            r.DataLimite, r.Observacoes, r.Usuario?.Nome ?? "",
            r.RetiradoEm, r.MotivoCancelamento, r.EstaExpirada(), r.CriadoEm,
            r.Itens.Select(i => new ItemReservaDto(i.Id, i.ProdutoId, i.ProdutoNome, i.Quantidade, i.PrecoUnitario, i.Total)).ToList()
        ));
    }
}
