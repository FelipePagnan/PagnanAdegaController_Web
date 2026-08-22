using System.Security.Claims;
using ERP.Adega.Application.Commands.Estoque;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Application.Queries.Estoque;
using ERP.Adega.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Adega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EstoqueController : ControllerBase
{
    private readonly IMediator _mediator;

    public EstoqueController(IMediator mediator) => _mediator = mediator;

    private Guid UsuarioId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    [HttpGet("{filialId:guid}")]
    public async Task<IActionResult> ObterPorFilial(
        Guid filialId,
        [FromQuery] string? termo,
        [FromQuery] NivelAlertaEstoque? nivelAlerta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ListarEstoqueQuery(filialId, termo, nivelAlerta, pagina, tamanhoPagina), ct);
        return Ok(result);
    }

    [HttpGet("{filialId:guid}/{produtoId:guid}")]
    public async Task<IActionResult> ObterSaldo(
        Guid filialId, Guid produtoId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ObterSaldoQuery(produtoId, filialId), ct);
        if (!result.Sucesso) return NotFound(new { result.Codigo, result.Erro });
        return Ok(result.Valor);
    }

    [HttpGet("alertas/{filialId:guid}")]
    public async Task<IActionResult> ObterAlertas(Guid filialId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListarAlertasQuery(filialId), ct);
        return Ok(result);
    }

    [HttpGet("lotes-vencendo/{filialId:guid}")]
    public async Task<IActionResult> LotesVencendo(
        Guid filialId, [FromQuery] int dias = 30, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListarLotesVencendoQuery(filialId, dias), ct);
        return Ok(result);
    }

    [HttpPost("entrada")]
    public async Task<IActionResult> Entrada(
        [FromBody] EntradaEstoqueRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new EntradaEstoqueCommand(
            request.ProdutoId, request.FilialId, request.Quantidade,
            request.CustoUnitario, request.CodigoLote, request.DataValidade,
            request.DataFabricacao, request.FornecedorId, request.NotaFiscal,
            UsuarioId), ct);

        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok(new { loteId = result.Valor });
    }

    [HttpPost("saida")]
    public async Task<IActionResult> Saida(
        [FromBody] SaidaEstoqueRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SaidaEstoqueCommand(
            request.ProdutoId, request.FilialId, request.Quantidade,
            request.Tipo, UsuarioId, request.Motivo, request.DocumentoOrigem), ct);

        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }

    [HttpPost("ajuste")]
    public async Task<IActionResult> AjustarEstoque(
        [FromBody] AjusteEstoqueRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new AjusteEstoqueCommand(
            request.ProdutoId, request.FilialId, request.NovaQuantidade,
            request.Motivo, UsuarioId), ct);

        if (!result.Sucesso) return UnprocessableEntity(new { result.Codigo, result.Erro });
        return Ok();
    }

    [HttpGet("movimentacoes/{produtoId:guid}/{filialId:guid}")]
    public async Task<IActionResult> ObterMovimentacoes(
        Guid produtoId, Guid filialId,
        [FromQuery] DateTime? inicio, [FromQuery] DateTime? fim,
        [FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ListarMovimentacoesQuery(produtoId, filialId, inicio, fim, pagina, tamanhoPagina), ct);
        return Ok(result);
    }
}

// === Request DTOs para os endpoints ===
public record EntradaEstoqueRequest(
    Guid ProdutoId,
    Guid FilialId,
    int Quantidade,
    decimal CustoUnitario,
    string? CodigoLote = null,
    DateTime? DataValidade = null,
    DateTime? DataFabricacao = null,
    Guid? FornecedorId = null,
    string? NotaFiscal = null
);

public record SaidaEstoqueRequest(
    Guid ProdutoId,
    Guid FilialId,
    int Quantidade,
    TipoMovimentacao Tipo,
    string? Motivo = null,
    string? DocumentoOrigem = null
);
