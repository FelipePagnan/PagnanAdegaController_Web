using System.Net;
using System.Text.Json;
using ERP.Adega.Domain.Exceptions;

namespace ERP.Adega.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            DomainException domainEx => (
                HttpStatusCode.UnprocessableEntity,
                new ErrorResponse(domainEx.Codigo, domainEx.Message)
            ),

            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("ARGUMENTO_INVALIDO", argEx.Message)
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                new ErrorResponse("NAO_AUTORIZADO", "Acesso não autorizado.")
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse("ERRO_INTERNO", "Ocorreu um erro interno. Tente novamente.")
            )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Erro não tratado: {Message}", exception.Message);
        else
            _logger.LogWarning("Erro de domínio: {Code} - {Message}", response.Codigo, response.Mensagem);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

public record ErrorResponse(string Codigo, string Mensagem);
