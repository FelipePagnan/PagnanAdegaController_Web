using ERP.Adega.Application.DTOs;
using ERP.Adega.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Adega.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);

        if (!result.Sucesso)
            return Unauthorized(new { result.Codigo, result.Erro });

        return Ok(result.Valor);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(refreshToken, ct);

        if (!result.Sucesso)
            return Unauthorized(new { result.Codigo, result.Erro });

        return Ok(result.Valor);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        // Extrai dados do token JWT
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var perfil = User.FindFirst("perfil")?.Value;
        var empresaId = User.FindFirst("empresa_id")?.Value;
        var permissoes = User.FindAll("permissao").Select(c => c.Value).ToList();
        var filiais = User.FindAll("filial_id").Select(c => c.Value).ToList();

        return Ok(new
        {
            Id = userId,
            Email = email,
            Perfil = perfil,
            EmpresaId = empresaId,
            Permissoes = permissoes,
            Filiais = filiais
        });
    }
}
