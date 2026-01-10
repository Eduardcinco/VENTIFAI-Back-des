using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace VentifyAPI.Middleware
{
    /// <summary>
    /// Middleware para loguear cookies entrantes y salientes (debugging)
    /// </summary>
    public class CookieDebugMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CookieDebugMiddleware> _logger;

        public CookieDebugMiddleware(RequestDelegate next, ILogger<CookieDebugMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            
            // Log incoming cookies
            _logger.LogInformation(
                "🔍 INCOMING REQUEST: {Method} {Path} | Cookies: {CookieCount}",
                request.Method,
                request.Path,
                request.Cookies.Count
            );

            if (request.Cookies.Count > 0)
            {
                foreach (var cookie in request.Cookies)
                {
                    var cookieValue = cookie.Value ?? string.Empty;
                    var displayValue = cookieValue.Substring(0, Math.Min(20, cookieValue.Length)) + "...";
                    _logger.LogInformation("  📥 Cookie: {Key} = {Value}", cookie.Key, displayValue);
                }
            }

            // Check CORS headers
            if (request.Headers.TryGetValue("Origin", out var originValue))
            {
                _logger.LogInformation("  🌐 Origin: {Origin}", originValue.ToString());
            }

            await _next(context);

            // Log outgoing cookies
            if (context.Response.Headers.ContainsKey("Set-Cookie"))
            {
                var setCookieHeaders = context.Response.Headers["Set-Cookie"];
                _logger.LogInformation(
                    "🍪 OUTGOING SET-COOKIE: {Count} cookies",
                    setCookieHeaders.Count
                );
                foreach (var cookie in setCookieHeaders)
                {
                    var displayCookie = cookie?.Substring(0, Math.Min(60, cookie?.Length ?? 0)) + "...";
                    _logger.LogInformation("  📤 {Cookie}", displayCookie ?? "null");
                }
            }

            // Log CORS headers
            if (context.Response.Headers.TryGetValue("Access-Control-Allow-Credentials", out var credentialsValue))
            {
                _logger.LogInformation(
                    "✅ Access-Control-Allow-Credentials: {Value}",
                    credentialsValue.ToString()
                );
            }
        }
    }
}
