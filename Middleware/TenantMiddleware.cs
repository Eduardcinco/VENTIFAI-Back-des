
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VentifyAPI.Services;

namespace VentifyAPI.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        // Rutas públicas que no requieren claim negocioId (login, register, swagger, health, dev tools)
        private static readonly string[] _publicPaths = new[]
        {
            "/", // Permitir acceso público a la raíz
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/refresh",
            "/api/auth/logout",
            "/api/auth/empleado",
            "/swagger",
            "/swagger/index.html",
            "/swagger/v1/swagger.json",
            "/health",
            "/favicon.ico",
            "/api/usuarios/dev/reset-password"
        };

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext ctx, ITenantContext tenant)
        {
            if (ctx.User?.Identity?.IsAuthenticated == true)
            {
                // Extraer negocioId del token
                var negocioClaim = ctx.User.FindFirst("negocioId")?.Value;
                if (int.TryParse(negocioClaim, out var nId))
                {
                    _logger.LogDebug("TenantMiddleware: got negocioId from token: {NegocioId}", nId);
                    tenant.NegocioId = nId;
                }
                else
                {
                    _logger.LogDebug("TenantMiddleware: token did not include negocioId claim");
                }

                // Extraer userId
                var userClaim = ctx.User.FindFirst("userId")?.Value
                                ?? ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userClaim, out var uId))
                {
                    _logger.LogDebug("TenantMiddleware: got userId from token: {UserId}", uId);
                    tenant.UserId = uId;
                }

                // Extraer rol
                tenant.Rol = ctx.User.FindFirst("rol")?.Value
                             ?? ctx.User.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(tenant.Rol))
                    _logger.LogDebug("TenantMiddleware: got rol: {Rol}", tenant.Rol);
            }

            // If token doesn't include negocioId claim but the user is authenticated and we have a userId, try to get negocio from DB as a fallback
            if (ctx.User?.Identity?.IsAuthenticated == true && tenant.NegocioId == null)
            {
                var userClaim = ctx.User.FindFirst("userId")?.Value
                                ?? ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userClaim, out var uId))
                {
                    try
                    {
                        var db = ctx.RequestServices.GetService(typeof(VentifyAPI.Data.AppDbContext)) as VentifyAPI.Data.AppDbContext;
                        if (db != null)
                        {
                            var user = await db.Usuarios.FindAsync(uId);
                            if (user != null && user.NegocioId.HasValue)
                            {
                                tenant.NegocioId = user.NegocioId.Value;
                                _logger.LogDebug("TenantMiddleware: got negocioId from DB for user {UserId}: {NegocioId}", uId, tenant.NegocioId);
                            }
                            else
                            {
                                _logger.LogDebug("TenantMiddleware: no negocioId found in DB for user {UserId}", uId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "TenantMiddleware: error while resolving negocioId from DB for user {UserId}", uId);
                    }
                }
            }

            // Si la ruta es pública, permitir aunque no exista negocioId (ej: login, register)
            var path = ctx.Request.Path.HasValue ? ctx.Request.Path.Value! : string.Empty;
            var isPublic = _publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            if (!isPublic && tenant.NegocioId == null)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("Falta claim negocioId");
                return;
            }

            await _next(ctx);
        }
    }
}
