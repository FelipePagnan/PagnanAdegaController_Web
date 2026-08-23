using System.Security.Claims;
using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ERP.Adega.Application.Common;

/// <summary>
/// Pipeline MediatR que intercepta todos os commands e grava auditoria automaticamente.
/// Captura: usuário, operação, data/hora, tipo do command.
/// </summary>
public class AuditoriaBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly IUnitOfWork _uow;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditoriaBehavior(IAuditoriaRepository auditoriaRepo, IUnitOfWork uow,
        IHttpContextAccessor httpContextAccessor)
    {
        _auditoriaRepo = auditoriaRepo;
        _uow = uow;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        // Só auditar commands (não queries) — commands alteram estado
        var requestName = typeof(TRequest).Name;
        if (!requestName.Contains("Command")) return response;

        try
        {
            var userId = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var empresaId = _httpContextAccessor.HttpContext?.User
                .FindFirstValue("empresa_id");

            var auditoria = Auditoria.Criar(
                operacao: requestName.Replace("Command", ""),
                entidade: requestName.Replace("Command", "").Split(new[] { "Criar", "Atualizar", "Cancelar", "Aprovar", "Rejeitar", "Receber", "Abrir", "Fechar", "Pagar", "Enviar", "Separar", "Retirar" }, StringSplitOptions.None).LastOrDefault() ?? requestName,
                usuarioId: userId != null ? Guid.Parse(userId) : Guid.Empty,
                empresaId: empresaId != null ? Guid.Parse(empresaId) : null,
                detalhes: $"Executado: {requestName}",
                ip: _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
            );

            await _auditoriaRepo.AdicionarAsync(auditoria, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Auditoria nunca deve impedir a operação principal
        }

        return response;
    }
}
