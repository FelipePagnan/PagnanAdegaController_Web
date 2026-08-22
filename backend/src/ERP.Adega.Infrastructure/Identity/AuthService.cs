using ERP.Adega.Application.Common;
using ERP.Adega.Application.DTOs;
using ERP.Adega.Application.Services;
using ERP.Adega.Domain.Interfaces;

namespace ERP.Adega.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IJwtService _jwtService;

    public AuthService(IUsuarioRepository usuarioRepo, IJwtService jwtService)
    {
        _usuarioRepo = usuarioRepo;
        _jwtService = jwtService;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepo.ObterPorEmailAsync(request.Email, ct);

        if (usuario is null || !VerificarSenha(request.Senha, usuario.SenhaHash))
            return Result.Fail<LoginResponse>("E-mail ou senha inválidos.", "CREDENCIAIS_INVALIDAS");

        if (!usuario.Ativo)
            return Result.Fail<LoginResponse>("Usuário inativo.", "USUARIO_INATIVO");

        var usuarioCompleto = await _usuarioRepo.ObterComPerfilAsync(usuario.Id, ct);
        if (usuarioCompleto?.Perfil is null)
            return Result.Fail<LoginResponse>("Perfil não encontrado.", "PERFIL_NAO_ENCONTRADO");

        var permissoes = usuarioCompleto.Perfil.Permissoes.ToList();
        var filiais = usuarioCompleto.FiliaisPermitidas.Select(f => f.FilialId).ToList();

        var token = _jwtService.GerarToken(
            usuario.Id, usuario.Email, usuarioCompleto.Perfil.Nome,
            usuario.EmpresaId, permissoes, filiais);

        var refreshToken = _jwtService.GerarRefreshToken();
        usuario.RegistrarLogin();

        var response = new LoginResponse(
            token, refreshToken, DateTime.UtcNow.AddHours(8),
            new UsuarioLogadoDto(
                usuario.Id, usuario.Nome, usuario.Email,
                usuarioCompleto.Perfil.Nome, usuario.EmpresaId,
                usuarioCompleto.Empresa?.RazaoSocial ?? "",
                permissoes, filiais));

        return Result.Ok(response);
    }

    public Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        => Task.FromResult(Result.Fail<LoginResponse>("Refresh token não implementado ainda.", "NAO_IMPLEMENTADO"));

    public string HashSenha(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);
    public bool VerificarSenha(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
}
