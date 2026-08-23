using System.Security.Claims;
using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Interfaces;
using ERP.Adega.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Adega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventarioController : ControllerBase
{
    private readonly IEstoqueProdutoRepository _estoqueRepo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;
    private readonly IUnitOfWork _uow;

    public InventarioController(IEstoqueProdutoRepository estoqueRepo,
        IMovimentacaoEstoqueRepository movRepo, IUnitOfWork uow)
    {
        _estoqueRepo = estoqueRepo;
        _movRepo = movRepo;
        _uow = uow;
    }

    /// <summary>
    /// Listar todos os produtos com saldo para contagem.
    /// </summary>
    [HttpGet("{filialId:guid}")]
    public async Task<IActionResult> ListarParaContagem(Guid filialId, CancellationToken ct)
    {
        var estoques = await _estoqueRepo.ObterPorFilialAsync(filialId, ct);
        var itens = estoques.Select(e => new
        {
            e.ProdutoId,
            ProdutoNome = e.Produto?.Nome ?? "",
            QuantidadeSistema = e.EstoqueFisico,
            QuantidadeContada = (int?)null,
            Divergencia = (int?)null,
            Status = "Pendente"
        }).ToList();

        return Ok(itens);
    }

    /// <summary>
    /// Registrar contagem em massa — recebe lista de produtos com quantidade contada.
    /// Calcula divergências e aplica ajustes rastreáveis.
    /// </summary>
    [HttpPost("{filialId:guid}")]
    [PermissaoRequerida("estoque.ajustar")]
    public async Task<IActionResult> RegistrarContagem(
        Guid filialId, [FromBody] RegistrarContagemRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var resultados = new List<object>();
        var totalAjustes = 0;

        foreach (var item in request.Itens)
        {
            var estoque = await _estoqueRepo.ObterAsync(item.ProdutoId, filialId, ct);
            if (estoque is null) continue;

            var divergencia = item.QuantidadeContada - estoque.EstoqueFisico;
            var status = divergencia == 0 ? "OK" : "Ajustado";

            if (divergencia != 0)
            {
                var saldoAnterior = estoque.EstoqueFisico;

                if (divergencia > 0)
                    estoque.Entrada(divergencia);
                else
                    estoque.Saida(Math.Abs(divergencia));

                await _movRepo.AdicionarAsync(MovimentacaoEstoque.Criar(
                    item.ProdutoId, filialId, TipoMovimentacao.Ajuste,
                    divergencia, saldoAnterior, estoque.EstoqueFisico, userId,
                    motivo: $"Inventário: {request.Motivo}. Sistema: {saldoAnterior}, Contado: {item.QuantidadeContada}",
                    documentoOrigem: $"Inventário {DateTime.UtcNow:yyyy-MM-dd}"), ct);

                totalAjustes++;
            }

            resultados.Add(new
            {
                item.ProdutoId,
                ProdutoNome = item.ProdutoNome,
                QuantidadeSistema = estoque.EstoqueFisico - (divergencia != 0 ? divergencia : 0),
                item.QuantidadeContada,
                Divergencia = divergencia,
                Status = status
            });
        }

        await _uow.SaveChangesAsync(ct);

        return Ok(new
        {
            totalProdutos = request.Itens.Count,
            totalAjustes,
            totalSemDivergencia = request.Itens.Count - totalAjustes,
            itens = resultados
        });
    }
}

public record RegistrarContagemRequest(
    string Motivo,
    List<ItemContagemRequest> Itens
);

public record ItemContagemRequest(
    Guid ProdutoId,
    string ProdutoNome,
    int QuantidadeContada
);
