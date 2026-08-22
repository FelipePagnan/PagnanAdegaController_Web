namespace ERP.Adega.Domain.Entities;

public class Usuario : EntityBase
{
    public string Nome { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string SenhaHash { get; private set; } = default!;
    public Guid PerfilId { get; private set; }
    public Guid EmpresaId { get; private set; }
    public bool Ativo { get; private set; } = true;
    public DateTime? UltimoLogin { get; private set; }

    // Navegação
    public Perfil Perfil { get; private set; } = default!;
    public Empresa Empresa { get; private set; } = default!;

    private readonly List<UsuarioFilial> _filiaisPermitidas = new();
    public IReadOnlyCollection<UsuarioFilial> FiliaisPermitidas => _filiaisPermitidas.AsReadOnly();

    private Usuario() { }

    public static Usuario Criar(string nome, string email, string senhaHash, Guid perfilId, Guid empresaId)
    {
        return new Usuario
        {
            Nome = nome.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            SenhaHash = senhaHash,
            PerfilId = perfilId,
            EmpresaId = empresaId
        };
    }

    public void RegistrarLogin() => UltimoLogin = DateTime.UtcNow;

    public void AlterarSenha(string novaSenhaHash)
    {
        SenhaHash = novaSenhaHash;
        MarcarAtualizado();
    }

    public void Atualizar(string nome, Guid perfilId)
    {
        Nome = nome.Trim();
        PerfilId = perfilId;
        MarcarAtualizado();
    }

    public void AdicionarFilial(Guid filialId)
    {
        if (!_filiaisPermitidas.Any(f => f.FilialId == filialId))
            _filiaisPermitidas.Add(new UsuarioFilial(Id, filialId));
    }

    public void RemoverFilial(Guid filialId)
    {
        var uf = _filiaisPermitidas.FirstOrDefault(f => f.FilialId == filialId);
        if (uf != null) _filiaisPermitidas.Remove(uf);
    }

    public bool TemAcessoFilial(Guid filialId)
        => _filiaisPermitidas.Any(f => f.FilialId == filialId);

    public void Inativar() { Ativo = false; MarcarAtualizado(); }
    public void Ativar() { Ativo = true; MarcarAtualizado(); }
}

public class UsuarioFilial
{
    public Guid UsuarioId { get; private set; }
    public Guid FilialId { get; private set; }

    public Usuario Usuario { get; private set; } = default!;
    public Filial Filial { get; private set; } = default!;

    private UsuarioFilial() { }

    internal UsuarioFilial(Guid usuarioId, Guid filialId)
    {
        UsuarioId = usuarioId;
        FilialId = filialId;
    }
}
