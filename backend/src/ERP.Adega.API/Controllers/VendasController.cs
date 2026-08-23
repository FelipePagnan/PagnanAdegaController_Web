using System.Security.Claims;
using ERP.Adega.Application.Commands.Vendas;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Application.Queries.Vendas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using ERP.Adega.API.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Adega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VendasController : ControllerBase
{
    private readonly IMediator _mediator;

    public VendasController(IMediator mediator) => _mediator = mediator;

    private Guid UsuarioId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    /// <summary>
    /// Criar e finalizar venda (PDV).
    /// Valida estoque, baixa FEFO, registra pagamentos.
    /// </summary>
    [HttpPost]
    [PermissaoRequerida("vendas.criar")]
    public async Task<IActionResult> Criar(
        [FromBody] CriarVendaRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CriarVendaCommand(request, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return CreatedAtAction(nameof(ObterPorId), new { id = result.Valor!.Id }, result.Valor);
    }

    /// <summary>
    /// Listar vendas da filial com filtros por data.
    /// </summary>
    [HttpGet("{filialId:guid}")]
    public async Task<IActionResult> Listar(
        Guid filialId,
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ListarVendasQuery(filialId, inicio, fim, pagina, tamanhoPagina), ct);
        return Ok(result);
    }

    /// <summary>
    /// Detalhe de uma venda com itens e pagamentos.
    /// </summary>
    [HttpGet("detalhe/{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ObterVendaQuery(id), ct);
        if (!result.Sucesso) return NotFound(new { result.Codigo, result.Erro });
        return Ok(result.Valor);
    }

    /// <summary>
    /// Cancelar venda — estorna estoque se já finalizada.
    /// </summary>
    [HttpPost("{id:guid}/cancelar")]
    [PermissaoRequerida("vendas.cancelar")]
    public async Task<IActionResult> Cancelar(
        Guid id, [FromBody] CancelarVendaRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CancelarVendaCommand(id, request.Motivo, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }

    /// <summary>
    /// Devolução parcial ou total — diferente de cancelamento.
    /// Devolve itens ao estoque.
    /// </summary>
    [HttpPost("{id:guid}/devolver")]
    public async Task<IActionResult> Devolver(
        Guid id, [FromBody] DevolucaoRequest request, CancellationToken ct)
    {
        var req = new DevolucaoRequest(id, request.Motivo, request.Itens);
        var result = await _mediator.Send(new CriarDevolucaoCommand(req, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok(new { devolucaoId = result.Valor });
    }

}
