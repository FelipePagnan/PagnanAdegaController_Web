using ERP.Adega.Application.Commands.Produtos;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Application.Queries.Produtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Adega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProdutosController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProdutosController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? termo,
        [FromQuery] Guid? categoriaId,
        [FromQuery] bool? ativo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ListarProdutosQuery(termo, categoriaId, ativo, pagina, tamanhoPagina), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ObterProdutoQuery(id), ct);
        if (!result.Sucesso) return NotFound(new { result.Codigo, result.Erro });
        return Ok(result.Valor);
    }

    [HttpGet("barcode/{codigo}")]
    public async Task<IActionResult> BuscarPorCodigoBarras(string codigo, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new BuscarPorBarcodeQuery(codigo), ct);
        if (!result.Sucesso) return NotFound(new { result.Codigo, result.Erro });
        return Ok(result.Valor);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarProdutoRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new CriarProdutoCommand(request), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return CreatedAtAction(nameof(ObterPorId), new { id = result.Valor }, new { id = result.Valor });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(
        Guid id, [FromBody] AtualizarProdutoRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new AtualizarProdutoCommand(id, request), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return NoContent();
    }

    [HttpPatch("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new InativarProdutoCommand(id), ct);
        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return NoContent();
    }
}
