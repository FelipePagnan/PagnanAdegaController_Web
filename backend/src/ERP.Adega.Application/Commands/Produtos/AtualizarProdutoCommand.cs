using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Commands.Produtos;

// === Atualizar Produto ===
public record AtualizarProdutoCommand(Guid Id, AtualizarProdutoRequest Request) : IRequest<Result>;

public class AtualizarProdutoCommandHandler : IRequestHandler<AtualizarProdutoCommand, Result>
{
    private readonly IProdutoRepository _produtoRepo;
    private readonly ICategoriaRepository _categoriaRepo;
    private readonly IUnitOfWork _uow;

    public AtualizarProdutoCommandHandler(IProdutoRepository produtoRepo, ICategoriaRepository categoriaRepo, IUnitOfWork uow)
    {
        _produtoRepo = produtoRepo;
        _categoriaRepo = categoriaRepo;
        _uow = uow;
    }

    public async Task<Result> Handle(AtualizarProdutoCommand cmd, CancellationToken ct)
    {
        var produto = await _produtoRepo.ObterComDetalhesAsync(cmd.Id, ct);
        if (produto is null)
            return Result.Fail("Produto não encontrado.", "PRODUTO_NAO_ENCONTRADO");

        var categoria = await _categoriaRepo.ObterPorIdAsync(cmd.Request.CategoriaId, ct);
        if (categoria is null)
            return Result.Fail("Categoria não encontrada.", "CATEGORIA_NAO_ENCONTRADA");

        var r = cmd.Request;
        produto.Atualizar(r.Nome, r.Descricao, r.CategoriaId, r.PrecoVenda,
            r.ControlaValidade, r.EstoqueMinimo, r.EstoqueCritico);

        _produtoRepo.Atualizar(produto);
        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

// === Inativar Produto (RN-017: não exclui, inativa) ===
public record InativarProdutoCommand(Guid Id) : IRequest<Result>;

public class InativarProdutoCommandHandler : IRequestHandler<InativarProdutoCommand, Result>
{
    private readonly IProdutoRepository _repo;
    private readonly IUnitOfWork _uow;

    public InativarProdutoCommandHandler(IProdutoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Result> Handle(InativarProdutoCommand cmd, CancellationToken ct)
    {
        var produto = await _repo.ObterPorIdAsync(cmd.Id, ct);
        if (produto is null)
            return Result.Fail("Produto não encontrado.", "PRODUTO_NAO_ENCONTRADO");

        produto.Inativar();
        _repo.Atualizar(produto);
        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
