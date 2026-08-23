using System.Security.Claims;
using ERP.Adega.Application.Commands.Financeiro;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Application.Queries.Financeiro;
using ERP.Adega.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Adega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CaixaController : ControllerBase
{
    private readonly IMediator _mediator;
    public CaixaController(IMediator mediator) => _mediator = mediator;

    private Guid UsuarioId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    [HttpGet("atual/{filialId:guid}")]
    public async Task<IActionResult> Atual(Guid filialId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ObterCaixaAtualQuery(filialId), ct);
        if (!result.Sucesso) return NotFound(new { result.Codigo, result.Erro });
        return Ok(result.Valor);
    }

    [HttpPost("abrir")]
    public async Task<IActionResult> Abrir([FromBody] AbrirCaixaRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new AbrirCaixaCommand(req.FilialId, req.SaldoAbertura, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok(result.Valor);
    }

    [HttpPost("fechar/{filialId:guid}")]
    public async Task<IActionResult> Fechar(Guid filialId, [FromBody] FecharCaixaRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new FecharCaixaCommand(filialId, req.Observacao, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok(result.Valor);
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FinanceiroController : ControllerBase
{
    private readonly IMediator _mediator;
    public FinanceiroController(IMediator mediator) => _mediator = mediator;

    private Guid UsuarioId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    // === Contas a Pagar ===

    [HttpGet("pagar/{filialId:guid}")]
    public async Task<IActionResult> ListarPagar(Guid filialId,
        [FromQuery] StatusConta? status, [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ListarContasPagarQuery(filialId, status, pagina, tamanhoPagina), ct);
        return Ok(result);
    }

    [HttpPost("pagar")]
    public async Task<IActionResult> CriarPagar([FromBody] CriarContaPagarRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new CriarContaPagarCommand(req), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok(new { id = result.Valor });
    }

    [HttpPost("pagar/{id:guid}/pagar")]
    public async Task<IActionResult> PagarConta(Guid id, [FromBody] PagarContaRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new PagarContaCommand(id, req.ValorPago, UsuarioId), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }

    // === Contas a Receber ===

    [HttpGet("receber/{filialId:guid}")]
    public async Task<IActionResult> ListarReceber(Guid filialId,
        [FromQuery] StatusConta? status, [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ListarContasReceberQuery(filialId, status, pagina, tamanhoPagina), ct);
        return Ok(result);
    }

    [HttpPost("receber")]
    public async Task<IActionResult> CriarReceber([FromBody] CriarContaReceberRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new CriarContaReceberCommand(req), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok(new { id = result.Valor });
    }

    [HttpPost("receber/{id:guid}/receber")]
    public async Task<IActionResult> ReceberConta(Guid id, CancellationToken ct)
    {
        // Recebe pelo valor original
        var result = await _mediator.Send(new ReceberContaCommand(id, 0), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }

    // === Fluxo de Caixa ===

    [HttpGet("fluxo/{filialId:guid}")]
    public async Task<IActionResult> FluxoCaixa(Guid filialId, CancellationToken ct)
    {
        var result = await _mediator.Send(new FluxoCaixaQuery(filialId), ct);
        return Ok(result);
    }
}
