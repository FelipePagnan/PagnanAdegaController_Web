using ERP.Adega.Domain.Enums;

namespace ERP.Adega.Domain.Entities;

public class CodigoBarras : EntityBase
{
    public Guid ProdutoId { get; private set; }
    public string Codigo { get; private set; } = default!;
    public TipoCodigoBarras Tipo { get; private set; }
    public bool Principal { get; private set; }

    // Navegação
    public Produto Produto { get; private set; } = default!;

    private CodigoBarras() { }

    internal CodigoBarras(Guid produtoId, string codigo, TipoCodigoBarras tipo, bool principal)
    {
        ProdutoId = produtoId;
        Codigo = codigo;
        Tipo = tipo;
        Principal = principal;
    }

    internal void DesmarcarPrincipal() => Principal = false;
}
