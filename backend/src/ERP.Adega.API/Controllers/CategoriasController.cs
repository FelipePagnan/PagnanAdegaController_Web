using ERP.Adega.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Adega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaRepository _repo;

    public CategoriasController(ICategoriaRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var categorias = await _repo.ObterAtivasAsync(ct);
        var result = categorias.Select(c => new { c.Id, c.Nome, c.Descricao }).ToList();
        return Ok(result);
    }
}
