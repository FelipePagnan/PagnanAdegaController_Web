namespace ERP.Adega.Application.DTOs;

public record LoginRequest(string Email, string Senha);

public record LoginResponse(
    string Token,
    string RefreshToken,
    DateTime Expiracao,
    UsuarioLogadoDto Usuario
);

public record UsuarioLogadoDto(
    Guid Id,
    string Nome,
    string Email,
    string Perfil,
    Guid EmpresaId,
    string EmpresaNome,
    IReadOnlyList<string> Permissoes,
    IReadOnlyList<Guid> FiliaisPermitidas
);
