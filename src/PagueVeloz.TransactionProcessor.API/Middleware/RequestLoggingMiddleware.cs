using System.Diagnostics;

namespace PagueVeloz.TransactionProcessor.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();

            context.Items["RequestId"] = requestId;

            _logger.LogInformation(
                "Solicitação inicial {RequestId} - {Method} {Path}",
                requestId, context.Request.Method, context.Request.Path);

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                _logger.LogInformation(
                    "Solicitação concluída {RequestId} - Status: {StatusCode} - Duração: {Duration}ms",
                    requestId, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
