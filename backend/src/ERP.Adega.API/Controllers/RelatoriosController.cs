using System.Globalization;
using System.Security.Claims;
using System.Text;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using ERP.Adega.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Adega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RelatoriosController : ControllerBase
{
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly IVendaRepository _vendaRepo;
    private readonly IContaPagarRepository _contaPagarRepo;
    private readonly IContaReceberRepository _contaReceberRepo;

    public RelatoriosController(IEstoqueProdutoRepository estoqueRepo, IVendaRepository vendaRepo,
        IContaPagarRepository contaPagarRepo, IContaReceberRepository contaReceberRepo)
    {
        _estoqueRepo = estoqueRepo;
        _vendaRepo = vendaRepo;
        _contaPagarRepo = contaPagarRepo;
        _contaReceberRepo = contaReceberRepo;
    }

    // ═══════════════════════════════════
    // RELATÓRIO DE ESTOQUE
    // ═══════════════════════════════════

    [HttpGet("estoque/{filialId:guid}")]
    public async Task<IActionResult> RelatorioEstoque(Guid filialId, CancellationToken ct)
    {
        var estoques = await _estoqueRepo.ObterPorFilialAsync(filialId, ct);
        var itens = estoques.Select(e => new
        {
            Produto = e.Produto?.Nome ?? "",
            e.EstoqueFisico,
            e.EstoqueReservado,
            EstoqueDisponivel = e.EstoqueDisponivel,
            ValorEstoque = e.EstoqueFisico * (e.Produto?.PrecoCusto ?? 0),
            Nivel = e.Produto?.CalcularAlerta(e.EstoqueDisponivel).ToString() ?? ""
        }).OrderBy(e => e.Produto).ToList();

        var totalValor = itens.Sum(i => i.ValorEstoque);
        return Ok(new { geradoEm = DateTime.UtcNow, totalProdutos = itens.Count, valorTotalEstoque = totalValor, itens });
    }

    [HttpGet("estoque/{filialId:guid}/csv")]
    public async Task<IActionResult> RelatorioEstoqueCsv(Guid filialId, CancellationToken ct)
    {
        var estoques = await _estoqueRepo.ObterPorFilialAsync(filialId, ct);
        var sb = new StringBuilder();
        sb.AppendLine("Produto;Físico;Reservado;Disponível;Valor Estoque;Nível Alerta");

        foreach (var e in estoques.OrderBy(e => e.Produto?.Nome))
        {
            var valor = e.EstoqueFisico * (e.Produto?.PrecoCusto ?? 0);
            var nivel = e.Produto?.CalcularAlerta(e.EstoqueDisponivel).ToString() ?? "";
            sb.AppendLine($"{e.Produto?.Nome};{e.EstoqueFisico};{e.EstoqueReservado};{e.EstoqueDisponivel};{valor:F2};{nivel}");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"estoque_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    // ═══════════════════════════════════
    // RELATÓRIO DE VENDAS
    // ═══════════════════════════════════

    [HttpGet("vendas/{filialId:guid}")]
    public async Task<IActionResult> RelatorioVendas(Guid filialId,
        [FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, CancellationToken ct)
    {
        var vendas = await _vendaRepo.ListarAsync(filialId, inicio, fim, 1, 1000, ct);
        var finalizadas = vendas.Where(v => v.Status == StatusVenda.Finalizada).ToList();
        var canceladas = vendas.Where(v => v.Status == StatusVenda.Cancelada).ToList();

        var totalVendas = finalizadas.Sum(v => v.Total);
        var ticketMedio = finalizadas.Count > 0 ? totalVendas / finalizadas.Count : 0;

        // Produtos mais vendidos
        var produtosMaisVendidos = finalizadas.SelectMany(v => v.Itens)
            .GroupBy(i => i.ProdutoNome)
            .Select(g => new { Produto = g.Key, Quantidade = g.Sum(i => i.Quantidade), Total = g.Sum(i => i.Total) })
            .OrderByDescending(p => p.Quantidade).Take(10).ToList();

        // Vendas por forma de pagamento
        var porFormaPagamento = finalizadas.SelectMany(v => v.Pagamentos)
            .GroupBy(p => p.Forma.ToString())
            .Select(g => new { Forma = g.Key, Total = g.Sum(p => p.Valor), Quantidade = g.Count() })
            .OrderByDescending(f => f.Total).ToList();

        return Ok(new
        {
            geradoEm = DateTime.UtcNow,
            periodo = new { inicio = inicio?.ToString("dd/MM/yyyy"), fim = fim?.ToString("dd/MM/yyyy") },
            totalVendasFinalizadas = finalizadas.Count,
            totalVendasCanceladas = canceladas.Count,
            valorTotalVendas = totalVendas,
            ticketMedio,
            produtosMaisVendidos,
            porFormaPagamento
        });
    }

    [HttpGet("vendas/{filialId:guid}/csv")]
    public async Task<IActionResult> RelatorioVendasCsv(Guid filialId,
        [FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, CancellationToken ct)
    {
        var vendas = await _vendaRepo.ListarAsync(filialId, inicio, fim, 1, 1000, ct);
        var sb = new StringBuilder();
        sb.AppendLine("Número;Data;Status;Itens;Subtotal;Desconto;Total;Operador;Pagamento Principal");

        foreach (var v in vendas)
        {
            var pgto = v.Pagamentos.FirstOrDefault()?.Forma.ToString() ?? "—";
            sb.AppendLine($"#{v.Numero};{v.CriadoEm:dd/MM/yyyy HH:mm};{v.Status};{v.TotalItens};{v.SubTotal:F2};{v.Desconto:F2};{v.Total:F2};{v.Usuario?.Nome ?? ""};{pgto}");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"vendas_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    // ═══════════════════════════════════
    // RELATÓRIO FINANCEIRO
    // ═══════════════════════════════════

    [HttpGet("financeiro/{filialId:guid}")]
    public async Task<IActionResult> RelatorioFinanceiro(Guid filialId, CancellationToken ct)
    {
        var totalPagar = await _contaPagarRepo.TotalAbertoAsync(filialId, ct);
        var totalReceber = await _contaReceberRepo.TotalAbertoAsync(filialId, ct);
        var contasPagar = await _contaPagarRepo.ListarAsync(filialId, null, 1, 100, ct);
        var contasReceber = await _contaReceberRepo.ListarAsync(filialId, null, 1, 100, ct);

        return Ok(new
        {
            geradoEm = DateTime.UtcNow,
            totalAPagar = totalPagar,
            totalAReceber = totalReceber,
            saldoProjetado = totalReceber - totalPagar,
            contasPagar = contasPagar.Select(c => new
            {
                c.Descricao, c.ValorOriginal, c.DataVencimento,
                Status = c.Status.ToString(), Vencida = c.EstaVencida(),
                Fornecedor = c.Fornecedor?.RazaoSocial
            }),
            contasReceber = contasReceber.Select(c => new
            {
                c.Descricao, c.ValorOriginal, c.DataVencimento,
                Status = c.Status.ToString(), Cliente = c.Cliente?.Nome
            })
        });
    }

    [HttpGet("financeiro/{filialId:guid}/csv")]
    public async Task<IActionResult> RelatorioFinanceiroCsv(Guid filialId, CancellationToken ct)
    {
        var contasPagar = await _contaPagarRepo.ListarAsync(filialId, null, 1, 100, ct);
        var contasReceber = await _contaReceberRepo.ListarAsync(filialId, null, 1, 100, ct);

        var sb = new StringBuilder();
        sb.AppendLine("Tipo;Descrição;Valor;Vencimento;Status;Fornecedor/Cliente");

        foreach (var c in contasPagar)
            sb.AppendLine($"Pagar;{c.Descricao};{c.ValorOriginal:F2};{c.DataVencimento:dd/MM/yyyy};{c.Status};{c.Fornecedor?.RazaoSocial ?? ""}");

        foreach (var c in contasReceber)
            sb.AppendLine($"Receber;{c.Descricao};{c.ValorOriginal:F2};{c.DataVencimento:dd/MM/yyyy};{c.Status};{c.Cliente?.Nome ?? ""}");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"financeiro_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
