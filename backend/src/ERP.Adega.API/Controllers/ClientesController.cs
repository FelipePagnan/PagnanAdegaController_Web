using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Interfaces;
using ERP.Adega.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Adega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly IClienteRepository _repo;
    private readonly IUnitOfWork _uow;

    public ClientesController(IClienteRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var todos = await _repo.ObterTodosAsync(ct);
        var result = todos.Select(c => new
        {
            c.Id, c.Nome, c.CPF, c.CNPJ,
            Telefone = c.Contato?.Telefone,
            Email = c.Contato?.Email,
            c.Ativo
        });
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var c = await _repo.ObterPorIdAsync(id, ct);
        if (c is null) return NotFound();
        return Ok(new
        {
            c.Id, c.Nome, c.CPF, c.CNPJ,
            c.Contato, c.Endereco, c.Observacoes, c.Ativo,
            c.CriadoEm, c.AtualizadoEm
        });
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarClienteRequest req, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(req.CPF))
        {
            var existente = await _repo.ObterPorCpfAsync(req.CPF, ct);
            if (existente != null)
                return UnprocessableEntity(new { codigo = "CPF_DUPLICADO", erro = "CPF já cadastrado." });
        }

        var cliente = Cliente.Criar(req.Nome, req.CPF, req.CNPJ);
        if (req.Telefone != null || req.Email != null)
        {
            cliente.Atualizar(req.Nome, req.CPF, req.CNPJ,
                new Contato(req.Telefone, null, req.Email, null),
                null, req.Observacoes);
        }

        await _repo.AdicionarAsync(cliente, ct);
        await _uow.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = cliente.Id }, new { id = cliente.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarClienteRequest req, CancellationToken ct)
    {
        var cliente = await _repo.ObterPorIdAsync(id, ct);
        if (cliente is null) return NotFound();

        cliente.Atualizar(req.Nome, req.CPF, req.CNPJ,
            new Contato(req.Telefone, null, req.Email, null),
            null, req.Observacoes);

        _repo.Atualizar(cliente);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken ct)
    {
        var cliente = await _repo.ObterPorIdAsync(id, ct);
        if (cliente is null) return NotFound();
        cliente.Inativar();
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record CriarClienteRequest(
    string Nome, string? CPF = null, string? CNPJ = null,
    string? Telefone = null, string? Email = null, string? Observacoes = null
);

public record AtualizarClienteRequest(
    string Nome, string? CPF = null, string? CNPJ = null,
    string? Telefone = null, string? Email = null, string? Observacoes = null
);
