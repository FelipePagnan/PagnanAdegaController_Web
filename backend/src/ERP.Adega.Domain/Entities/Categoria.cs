namespace ERP.Adega.Domain.Entities;

public class Categoria : EntityBase
{
    public string Nome { get; private set; } = default!;
    public string? Descricao { get; private set; }
    public Guid? CategoriaPaiId { get; private set; }
    public bool Ativa { get; private set; } = true;

    // Navegação
    public Categoria? CategoriaPai { get; private set; }
    private readonly List<Categoria> _subcategorias = new();
    public IReadOnlyCollection<Categoria> Subcategorias => _subcategorias.AsReadOnly();

    private Categoria() { }

    public static Categoria Criar(string nome, string? descricao = null, Guid? categoriaPaiId = null)
    {
        return new Categoria
        {
            Nome = nome.Trim(),
            Descricao = descricao?.Trim(),
            CategoriaPaiId = categoriaPaiId
        };
    }

    public void Atualizar(string nome, string? descricao)
    {
        Nome = nome.Trim();
        Descricao = descricao?.Trim();
        MarcarAtualizado();
    }

    public void Inativar() { Ativa = false; MarcarAtualizado(); }
}
