using System.Security.Claims;
using ERP.Adega.Application.Commands.Reservas;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Application.Queries.Reservas;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Adega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservasController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReservasController(IMediator mediator) => _mediator = mediator;

    private Guid UsuarioId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    [HttpGet("{filialId:guid}")]
    public async Task<IActionResult> Listar(Guid filialId,
        [FromQuery] StatusReserva? status, [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListarReservasQuery(filialId, status, pagina, tamanhoPagina), ct);
        return Ok(result);
    }

    [HttpGet("detalhe/{id:guid}")]
    public async Task<IActionResult> Detalhe(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ObterReservaQuery(id), ct);
        if (!result.Sucesso) return NotFound(new { result.Codigo, result.Erro });
        return Ok(result.Valor);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarReservaRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CriarReservaCommand(request, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok(new { id = result.Valor });
    }

    [HttpPost("{id:guid}/retirar")]
    public async Task<IActionResult> Retirar(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new RetirarReservaCommand(id, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, [FromBody] CancelarReservaRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelarReservaCommand(id, request.Motivo), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }
}

// ═══════════════════════════════════════
// NOTIFICAÇÕES — Agrega alertas do sistema
// ═══════════════════════════════════════
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificacoesController : ControllerBase
{
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly ILoteRepository _loteRepo;
    private readonly IPedidoCompraRepository _pedidoRepo;
    private readonly IContaPagarRepository _contaPagarRepo;
    private readonly IReservaRepository _reservaRepo;

    public NotificacoesController(IEstoqueProdutoRepository estoqueRepo, ILoteRepository loteRepo,
        IPedidoCompraRepository pedidoRepo, IContaPagarRepository contaPagarRepo, IReservaRepository reservaRepo)
    {
        _estoqueRepo = estoqueRepo;
        _loteRepo = loteRepo;
        _pedidoRepo = pedidoRepo;
        _contaPagarRepo = contaPagarRepo;
        _reservaRepo = reservaRepo;
    }

    [HttpGet("{filialId:guid}")]
    public async Task<IActionResult> Listar(Guid filialId, CancellationToken ct)
    {
        var notificacoes = new List<object>();

        // Estoque crítico/baixo
        var estoques = await _estoqueRepo.ObterPorFilialAsync(filialId, ct);
        foreach (var e in estoques)
        {
            var nivel = e.Produto.CalcularAlerta(e.EstoqueDisponivel);
            if (nivel == NivelAlertaEstoque.Critico)
                notificacoes.Add(new { tipo = "estoque_critico", titulo = $"Estoque Crítico: {e.Produto.Nome}",
                    detalhe = $"{e.EstoqueDisponivel} unidades disponíveis", prioridade = "alta", data = DateTime.UtcNow });
            else if (nivel == NivelAlertaEstoque.Baixo)
                notificacoes.Add(new { tipo = "estoque_baixo", titulo = $"Estoque Baixo: {e.Produto.Nome}",
                    detalhe = $"{e.EstoqueDisponivel} unidades disponíveis", prioridade = "media", data = DateTime.UtcNow });
        }

        // Lotes vencendo
        var lotesVencendo = await _loteRepo.ObterVencendoAsync(filialId, 30, ct);
        foreach (var l in lotesVencendo)
        {
            var label = l.EstaVencido() ? "Vencido" : "Vencendo";
            notificacoes.Add(new { tipo = l.EstaVencido() ? "lote_vencido" : "lote_vencendo",
                titulo = $"Lote {label}: {l.Codigo}",
                detalhe = $"Validade: {l.DataValidade?.ToString("dd/MM/yyyy")}, {l.QuantidadeAtual} un restantes",
                prioridade = l.EstaVencido() ? "alta" : "media", data = l.DataValidade });
        }

        // Compras pendentes
        var pendentes = await _pedidoRepo.ContarPendentesAsync(filialId, ct);
        if (pendentes > 0)
            notificacoes.Add(new { tipo = "compra_pendente",
                titulo = $"{pendentes} pedido(s) aguardando aprovação",
                detalhe = "Acesse Compras para aprovar ou rejeitar", prioridade = "media", data = DateTime.UtcNow });

        // Contas vencidas
        var contasVencidas = await _contaPagarRepo.ContarVencidasAsync(filialId, ct);
        if (contasVencidas > 0)
            notificacoes.Add(new { tipo = "conta_vencida",
                titulo = $"{contasVencidas} conta(s) a pagar vencida(s)",
                detalhe = "Acesse Financeiro para regularizar", prioridade = "alta", data = DateTime.UtcNow });

        // Reservas expirando
        var reservas = await _reservaRepo.ListarAsync(filialId, StatusReserva.Ativa, 1, 100, ct);
        foreach (var r in reservas.Where(r => r.EstaExpirada()))
            notificacoes.Add(new { tipo = "reserva_expirada",
                titulo = $"Reserva #{r.Numero} expirada",
                detalhe = $"Cliente: {r.Cliente?.Nome}, Prazo: {r.DataLimite:dd/MM/yyyy}",
                prioridade = "media", data = r.DataLimite });

        return Ok(new { total = notificacoes.Count, itens = notificacoes.OrderByDescending(n => ((dynamic)n).prioridade == "alta").ToList() });
    }
}
