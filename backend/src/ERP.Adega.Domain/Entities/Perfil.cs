namespace ERP.Adega.Domain.Entities;

public class Perfil : EntityBase
{
    public string Nome { get; private set; } = default!;
    public string? Descricao { get; private set; }
    public Guid EmpresaId { get; private set; }
    public bool Sistema { get; private set; }

    private readonly List<string> _permissoes = new();
    public IReadOnlyCollection<string> Permissoes => _permissoes.AsReadOnly();

    // Navegação
    public Empresa Empresa { get; private set; } = default!;

    private Perfil() { }

    public static Perfil Criar(string nome, Guid empresaId, string? descricao = null, bool sistema = false)
    {
        return new Perfil
        {
            Nome = nome.Trim(),
            EmpresaId = empresaId,
            Descricao = descricao?.Trim(),
            Sistema = sistema
        };
    }

    public void AdicionarPermissao(string permissao)
    {
        var p = permissao.Trim().ToLowerInvariant();
        if (!_permissoes.Contains(p))
            _permissoes.Add(p);
    }

    public void RemoverPermissao(string permissao)
    {
        _permissoes.Remove(permissao.Trim().ToLowerInvariant());
    }

    public void DefinirPermissoes(IEnumerable<string> permissoes)
    {
        _permissoes.Clear();
        foreach (var p in permissoes)
            AdicionarPermissao(p);
    }

    public bool PossuiPermissao(string permissao)
        => _permissoes.Contains(permissao.Trim().ToLowerInvariant());
}
