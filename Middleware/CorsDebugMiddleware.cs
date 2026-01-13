using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace VentifyAPI.Middleware
{
    public class CorsDebugMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorsDebugMiddleware> _logger;

        public CorsDebugMiddleware(RequestDelegate next, ILogger<CorsDebugMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var origin = context.Request.Headers["Origin"].ToString();
            _logger.LogInformation($"[CORS DEBUG] Origin: {origin}");
            await _next(context);
        }
    }
}
