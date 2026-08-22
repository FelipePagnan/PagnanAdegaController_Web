using System.Security.Claims;
using ERP.Adega.Application.Commands.Compras;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Application.Queries.Compras;
using ERP.Adega.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Adega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComprasController : ControllerBase
{
    private readonly IMediator _mediator;
    public ComprasController(IMediator mediator) => _mediator = mediator;

    private Guid UsuarioId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    [HttpGet("{filialId:guid}")]
    public async Task<IActionResult> Listar(
        Guid filialId,
        [FromQuery] StatusPedidoCompra? status,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ListarPedidosCompraQuery(filialId, status, pagina, tamanhoPagina), ct);
        return Ok(result);
    }

    [HttpGet("detalhe/{id:guid}")]
    public async Task<IActionResult> Detalhe(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ObterPedidoCompraQuery(id), ct);
        if (!result.Sucesso) return NotFound(new { result.Codigo, result.Erro });
        return Ok(result.Valor);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarPedidoCompraRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CriarPedidoCompraCommand(request, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return CreatedAtAction(nameof(Detalhe), new { id = result.Valor }, new { id = result.Valor });
    }

    [HttpPost("{id:guid}/aprovar")]
    public async Task<IActionResult> Aprovar(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new AprovarPedidoCommand(id, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }

    [HttpPost("{id:guid}/rejeitar")]
    public async Task<IActionResult> Rejeitar(
        Guid id, [FromBody] RejeicaoRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RejeitarPedidoCommand(id, UsuarioId, request.Motivo), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }

    [HttpPost("{id:guid}/receber")]
    public async Task<IActionResult> Receber(
        Guid id, [FromBody] ReceberPedidoRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReceberPedidoCommand(id, request, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelarPedidoCompraCommand(id), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }
}
