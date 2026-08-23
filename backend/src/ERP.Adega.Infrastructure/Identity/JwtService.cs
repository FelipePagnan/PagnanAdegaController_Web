using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ERP.Adega.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Adega.Infrastructure.Identity;

public interface IJwtService
{
    string GerarToken(Usuario usuario);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config) => _config = config;

    public string GerarToken(Usuario usuario)
    {
        var secret = _config["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email),
            new("empresa_id", usuario.EmpresaId.ToString()),
            new("perfil", usuario.Perfil?.Nome ?? ""),
        };

        // Adicionar filiais
        foreach (var uf in usuario.Filiais)
            claims.Add(new Claim("filial_id", uf.FilialId.ToString()));

        // Adicionar permissões individualmente para o filtro de autorização
        if (usuario.Perfil != null)
        {
            foreach (var permissao in usuario.Perfil.Permissoes)
                claims.Add(new Claim("permissao", permissao));
        }

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
