using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;

namespace ERP.Adega.Application.Services;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    string HashSenha(string senha);
    bool VerificarSenha(string senha, string hash);
}

public interface IJwtService
{
    string GerarToken(Guid usuarioId, string email, string perfil, Guid empresaId,
        IEnumerable<string> permissoes, IEnumerable<Guid> filiais);
    string GerarRefreshToken();
}
