using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERP.Adega.API.Filters;

/// <summary>
/// Filtro de autorização por permissão.
/// Uso: [PermissaoRequerida("vendas.criar")]
/// Verifica se o JWT do usuário contém a permissão no claim "permissao".
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class PermissaoRequeridaAttribute : TypeFilterAttribute
{
    public PermissaoRequeridaAttribute(string permissao)
        : base(typeof(PermissaoRequeridaFilter))
    {
        Arguments = new object[] { permissao };
    }
}

public class PermissaoRequeridaFilter : IAuthorizationFilter
{
    private readonly string _permissao;

    public PermissaoRequeridaFilter(string permissao)
    {
        _permissao = permissao;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Buscar permissões nos claims
        var permissoes = user.FindAll("permissao").Select(c => c.Value).ToList();

        // Admin tem acesso total
        if (permissoes.Contains("*") || permissoes.Contains("admin"))
            return;

        if (!permissoes.Contains(_permissao))
        {
            context.Result = new ObjectResult(new
            {
                codigo = "SEM_PERMISSAO",
                erro = $"Permissão necessária: {_permissao}"
            })
            { StatusCode = 403 };
        }
    }
}
