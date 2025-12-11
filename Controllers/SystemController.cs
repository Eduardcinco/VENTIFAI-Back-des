using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VentifyAPI.Data;

namespace VentifyAPI.Controllers
{
    [Route("api/system")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SystemController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/system/time
        [HttpGet("time")]
        public IActionResult GetServerTime()
        {
            return Ok(new
            {
                nowUtc = DateTime.UtcNow
            });
        }

        // GET: api/system/session
        // Devuelve información básica de sesión a partir del token (incluido vía cookie HttpOnly)
        [Authorize]
        [HttpGet("session")]
        public async Task<IActionResult> GetSession()
        {
            var user = HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true) return Unauthorized();
            
            var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            // Buscar usuario para obtener permisos extra
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null) return Unauthorized();

            // Parsear permisos extra
            var permisosExtra = new List<string>();
            if (!string.IsNullOrEmpty(usuario.PermisosExtra))
            {
                try
                {
                    permisosExtra = System.Text.Json.JsonSerializer.Deserialize<List<string>>(usuario.PermisosExtra) ?? new List<string>();
                }
                catch { }
            }

            // Obtener nombre del asignador si existe
            string? asignadoPorNombre = null;
            if (usuario.PermisosExtraAsignadoPor.HasValue)
            {
                var asignador = await _context.Usuarios.FindAsync(usuario.PermisosExtraAsignadoPor.Value);
                asignadoPorNombre = asignador?.Nombre;
            }

            var rol = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("rol")?.Value;
            var negocioId = user.FindFirst("negocioId")?.Value;
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            var name = user.FindFirst(ClaimTypes.Name)?.Value;

            return Ok(new { 
                isAuthenticated = true,
                userId = userIdStr, 
                rol, 
                negocioId, 
                email, 
                name,
                primerAcceso = usuario.PrimerAcceso,
                permisosExtra = new {
                    modulos = permisosExtra,
                    asignadoPor = asignadoPorNombre,
                    asignadoEn = usuario.PermisosExtraFecha,
                    nota = usuario.PermisosExtraNota
                }
            });
        }
    }
}
