using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Interfaces;
using ERP.Adega.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Adega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FornecedoresController : ControllerBase
{
    private readonly IFornecedorRepository _repo;
    private readonly IUnitOfWork _uow;

    public FornecedoresController(IFornecedorRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool? ativo, CancellationToken ct)
    {
        var todos = ativo == true
            ? await _repo.ObterAtivosAsync(ct)
            : await _repo.ObterTodosAsync(ct);

        var result = todos.Select(f => new
        {
            f.Id, f.RazaoSocial, f.NomeFantasia, f.CNPJ,
            ContatoTelefone = f.Contato?.Telefone,
            ContatoEmail = f.Contato?.Email,
            f.Ativo
        });
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var f = await _repo.ObterPorIdAsync(id, ct);
        if (f is null) return NotFound();
        return Ok(new
        {
            f.Id, f.RazaoSocial, f.NomeFantasia, f.CNPJ,
            f.Contato, f.Endereco, f.Observacoes, f.Ativo,
            f.CriadoEm, f.AtualizadoEm
        });
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarFornecedorRequest req, CancellationToken ct)
    {
        var existente = await _repo.ObterPorCnpjAsync(req.CNPJ, ct);
        if (existente != null)
            return UnprocessableEntity(new { codigo = "CNPJ_DUPLICADO", erro = "CNPJ já cadastrado." });

        var fornecedor = Fornecedor.Criar(req.RazaoSocial, req.CNPJ, req.NomeFantasia);

        if (req.Telefone != null || req.Email != null)
        {
            fornecedor.Atualizar(req.RazaoSocial, req.NomeFantasia,
                new Contato(req.Telefone, null, req.Email, req.NomeContato),
                null, req.Observacoes);
        }

        await _repo.AdicionarAsync(fornecedor, ct);
        await _uow.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ObterPorId), new { id = fornecedor.Id }, new { id = fornecedor.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarFornecedorRequest req, CancellationToken ct)
    {
        var fornecedor = await _repo.ObterPorIdAsync(id, ct);
        if (fornecedor is null) return NotFound();

        fornecedor.Atualizar(req.RazaoSocial, req.NomeFantasia,
            new Contato(req.Telefone, null, req.Email, req.NomeContato),
            null, req.Observacoes);

        _repo.Atualizar(fornecedor);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken ct)
    {
        var fornecedor = await _repo.ObterPorIdAsync(id, ct);
        if (fornecedor is null) return NotFound();
        fornecedor.Inativar();
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record CriarFornecedorRequest(
    string RazaoSocial,
    string CNPJ,
    string? NomeFantasia = null,
    string? Telefone = null,
    string? Email = null,
    string? NomeContato = null,
    string? Observacoes = null
);

public record AtualizarFornecedorRequest(
    string RazaoSocial,
    string? NomeFantasia = null,
    string? Telefone = null,
    string? Email = null,
    string? NomeContato = null,
    string? Observacoes = null
);
