using System.Net;
using System.Text.Json;

namespace PagueVeloz.TransactionProcessor.API.Middleware
{
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
                _logger.LogError(ex, "Ocorreu uma exceção não tratada");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            int statusCode;
            string message;

            if (exception is ArgumentException or ArgumentNullException)
            {
                statusCode = (int)HttpStatusCode.BadRequest;
                message = "Parâmetros de solicitação inválidos";
            }
            else if (exception is InvalidOperationException)
            {
                statusCode = (int)HttpStatusCode.Conflict;
                message = "A operação não pode ser executada";
            }
            else if (exception is KeyNotFoundException)
            {
                statusCode = (int)HttpStatusCode.NotFound;
                message = "Recurso não encontrado";
            }
            else
            {
                statusCode = (int)HttpStatusCode.InternalServerError;
                message = "Ocorreu um erro ao processar sua solicitação.";
            }

            context.Response.StatusCode = statusCode;

            var response = new
            {
                error = new
                {
                    message = message,
                    type = exception.GetType().Name,
                    details = exception.Message
                },
                traceId = context.TraceIdentifier,
                timestamp = DateTime.UtcNow
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
