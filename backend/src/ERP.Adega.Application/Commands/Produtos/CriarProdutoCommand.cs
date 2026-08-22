using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Interfaces;
using MediatR;

namespace ERP.Adega.Application.Commands.Produtos;

public record CriarProdutoCommand(CriarProdutoRequest Request) : IRequest<Result<Guid>>;

public class CriarProdutoCommandHandler : IRequestHandler<CriarProdutoCommand, Result<Guid>>
{
    private readonly IProdutoRepository _produtoRepo;
    private readonly ICategoriaRepository _categoriaRepo;
    private readonly IUnitOfWork _uow;

    public CriarProdutoCommandHandler(
        IProdutoRepository produtoRepo,
        ICategoriaRepository categoriaRepo,
        IUnitOfWork uow)
    {
        _produtoRepo = produtoRepo;
        _categoriaRepo = categoriaRepo;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(CriarProdutoCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        // Validar categoria
        var categoria = await _categoriaRepo.ObterPorIdAsync(req.CategoriaId, ct);
        if (categoria is null)
            return Result.Fail<Guid>("Categoria não encontrada.", "CATEGORIA_NAO_ENCONTRADA");

        // Criar produto
        var produto = Produto.Criar(
            req.Nome,
            req.CategoriaId,
            req.UnidadeBase,
            req.PrecoVenda,
            req.ControlaValidade,
            req.Descricao,
            req.EstoqueMinimo,
            req.EstoqueCritico
        );

        // Códigos de barras
        if (req.CodigosBarras != null)
        {
            foreach (var cb in req.CodigosBarras)
            {
                // Verificar unicidade
                var existente = await _produtoRepo.ObterPorCodigoBarrasAsync(cb.Codigo, ct);
                if (existente != null)
                    return Result.Fail<Guid>($"Código de barras '{cb.Codigo}' já está em uso.", "CODIGO_BARRAS_DUPLICADO");

                produto.AdicionarCodigoBarras(cb.Codigo, cb.Tipo, cb.Principal);
            }
        }

        // Embalagens (RN-007)
        if (req.Embalagens != null)
        {
            foreach (var emb in req.Embalagens)
            {
                produto.AdicionarEmbalagem(emb.Nome, emb.QuantidadeUnidades, emb.CodigoBarras, emb.PrecoSugerido);
            }
        }

        await _produtoRepo.AdicionarAsync(produto, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Ok(produto.Id);
    }
}
