using System.Security.Claims;
using ERP.Adega.Application.Commands.Transferencias;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Application.Queries.Transferencias;
using ERP.Adega.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Adega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransferenciasController : ControllerBase
{
    private readonly IMediator _mediator;
    public TransferenciasController(IMediator mediator) => _mediator = mediator;

    private Guid UsuarioId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] Guid? filialId,
        [FromQuery] StatusTransferencia? status, [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListarTransferenciasQuery(filialId, status, pagina, tamanhoPagina), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalhe(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ObterTransferenciaQuery(id), ct);
        if (!result.Sucesso) return NotFound(new { result.Codigo, result.Erro });
        return Ok(result.Valor);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarTransferenciaRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CriarTransferenciaCommand(request, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok(new { id = result.Valor });
    }

    [HttpPost("{id:guid}/aprovar")]
    public async Task<IActionResult> Aprovar(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new AprovarTransferenciaCommand(id, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }

    [HttpPost("{id:guid}/separar")]
    public async Task<IActionResult> Separar(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new SepararTransferenciaCommand(id), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }

    [HttpPost("{id:guid}/enviar")]
    public async Task<IActionResult> Enviar(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new EnviarTransferenciaCommand(id, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }

    [HttpPost("{id:guid}/receber")]
    public async Task<IActionResult> Receber(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReceberTransferenciaCommand(id, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, [FromBody] CancelarTransferenciaRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelarTransferenciaCommand(id, request.Motivo), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }
}
