using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentifyAPI.Data;
using VentifyAPI.Models;
using VentifyAPI.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace VentifyAPI.Controllers
{
    /// <summary>
    /// Autenticación y gestión de usuarios
    /// Build: 2025-01-13 - Rutas normalizadas a minúsculas
    /// </summary>
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly VentifyAPI.Services.ITenantContext _tenant;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, ITokenService tokenService, VentifyAPI.Services.ITenantContext tenant, ILogger<AuthController> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _tenant = tenant;
            _logger = logger;
        }

        private static CookieOptions BuildCookieOptions(DateTime? expiresAt)
        {
            var isDev = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);
            
            // En desarrollo: SameSite=Lax (localhost), Secure=false
            // En producciÃ³n (Railway): SameSite=None, Secure=true (HTTPS obligatorio)
            // NO usar Domain en Railway - deja que el navegador maneje el dominio
            var opts = new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDev,              // Secure=true en Railway (HTTPS), false en dev
                SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None,
                Path = "/",
                Expires = expiresAt
            };
            return opts;
        }

        // ================================
        //       REGISTRO
        // POST: api/auth/register
        // ================================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] DTOs.RegistroUsuarioDTO usuarioDto)
        {
            // ValidaciÃ³n simple
            if (string.IsNullOrEmpty(usuarioDto.Nombre) ||
                string.IsNullOrEmpty(usuarioDto.Correo) ||
                string.IsNullOrEmpty(usuarioDto.Password))
            {
                return BadRequest(new { message = "Todos los campos son obligatorios." });
            }

            // Verificar si el correo ya existe
            var existe = await _context.Usuarios
                .AnyAsync(u => u.Correo == usuarioDto.Correo);

            if (existe)
            {
                return BadRequest(new { message = "El correo ya estÃ¡ registrado." });
            }

            // 1) Crear usuario (dueÃ±o) sin negocio inicialmente
            var newUser = new Usuario
            {
                Nombre = usuarioDto.Nombre,
                Correo = usuarioDto.Correo,
                Password = BCrypt.Net.BCrypt.HashPassword(usuarioDto.Password),
                Rol = "dueÃ±o",
                NegocioId = null
            };
            _context.Usuarios.Add(newUser);
            await _context.SaveChangesAsync();

            // 2) Crear negocio del dueÃ±o y asociar OwnerId
            var negocio = new Negocio
            {
                NombreNegocio = string.IsNullOrWhiteSpace(usuarioDto.Nombre) ? "Mi Negocio" : $"{usuarioDto.Nombre} - Negocio",
                OwnerId = newUser.Id,
                CreadoEn = DateTime.UtcNow
            };
            _context.Negocios.Add(negocio);
            await _context.SaveChangesAsync();

            // 3) Actualizar NegocioId del usuario
            newUser.NegocioId = negocio.Id;
            await _context.SaveChangesAsync();

            var (accessToken, refreshToken, expiresAt) = _tokenService.CreateTokens(newUser);
            var rt = new RefreshToken
            {
                Token = refreshToken,
                UsuarioId = newUser.Id,
                ExpiresAt = expiresAt,
                Revoked = false
            };
            _context.RefreshTokens.Add(rt);
            await _context.SaveChangesAsync();

            // Setear cookies HttpOnly (access + refresh)
            Response.Cookies.Append("access_token", accessToken, BuildCookieOptions(expiresAt));
            Response.Cookies.Append("refresh_token", refreshToken, BuildCookieOptions(expiresAt.AddDays(7)));

            return Ok(new
            {
                message = "Usuario registrado correctamente.",
                accessToken,
                refreshToken,
                expiresAt,
                usuario = new
                {
                    newUser.Id,
                    newUser.Nombre,
                    newUser.Correo,
                    newUser.Rol,
                    NegocioId = newUser.NegocioId
                }
            });
        }

        // ================================
        //       LOGIN
        // POST: api/auth/login
        // ================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] DTOs.LoginDTO loginInfo)
        {
            try
            {
                if (loginInfo == null || string.IsNullOrWhiteSpace(loginInfo.CorreoNormalizado) || string.IsNullOrWhiteSpace(loginInfo.Password))
                {
                    return BadRequest(new { message = "Correo/Email y Password son requeridos." });
                }
                // Buscar usuario - IGNORAR filtro de tenant porque aÃºn no estÃ¡ autenticado
                var usuario = await _context.Usuarios
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Correo == loginInfo.CorreoNormalizado);

                if (usuario == null)
            {
                return Unauthorized(new { message = "Correo o contraseÃ±a incorrectos." });
            }

            // Comparar contraseÃ±a usando BCrypt
            if (!BCrypt.Net.BCrypt.Verify(loginInfo.Password, usuario.Password))
            {
                return Unauthorized(new { message = "Correo o contraseÃ±a incorrectos." });
            }

            // Auto-reparaciÃ³n: si es dueÃ±o y no tiene NegocioId, crear negocio ahora
            if (string.Equals(usuario.Rol, "dueÃ±o", StringComparison.OrdinalIgnoreCase) && usuario.NegocioId == null)
            {
                var fixNegocio = new Negocio
                {
                    NombreNegocio = string.IsNullOrWhiteSpace(usuario.Nombre) ? "Mi Negocio" : $"{usuario.Nombre} - Negocio",
                    OwnerId = usuario.Id,
                    CreadoEn = DateTime.UtcNow
                };
                _context.Negocios.Add(fixNegocio);
                await _context.SaveChangesAsync();
                usuario.NegocioId = fixNegocio.Id;
                await _context.SaveChangesAsync();
            }

            var (accessToken, refreshToken, expiresAt) = _tokenService.CreateTokens(usuario);
            var rt = new RefreshToken
            {
                Token = refreshToken,
                UsuarioId = usuario.Id,
                ExpiresAt = expiresAt,
                Revoked = false
            };
            _context.RefreshTokens.Add(rt);
            await _context.SaveChangesAsync();

            // Setear cookies HttpOnly (access + refresh)
            Response.Cookies.Append("access_token", accessToken, BuildCookieOptions(expiresAt));
            Response.Cookies.Append("refresh_token", refreshToken, BuildCookieOptions(expiresAt.AddDays(7)));

            // Parsear permisos extra
            var modulosExtra = new List<string>();
            if (!string.IsNullOrEmpty(usuario.PermisosExtra))
            {
                try
                {
                    modulosExtra = System.Text.Json.JsonSerializer.Deserialize<List<string>>(usuario.PermisosExtra) ?? new List<string>();
                }
                catch { }
            }

            // Obtener nombre del asignador
            string? asignadoPorNombre = null;
            if (usuario.PermisosExtraAsignadoPor.HasValue)
            {
                var asignador = await _context.Usuarios.FindAsync(usuario.PermisosExtraAsignadoPor.Value);
                asignadoPorNombre = asignador?.Nombre ?? asignador?.Correo;
            }

            return Ok(new
            {
                message = "Inicio de sesiÃ³n correcto.",
                accessToken,
                refreshToken,
                expiresAt,
                primerAcceso = usuario.PrimerAcceso,
                usuario = new
                {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Correo,
                    usuario.Rol,
                    fotoPerfilUrl = usuario.FotoPerfil,
                    NegocioId = usuario.NegocioId,
                    permisosExtra = new
                    {
                        modulos = modulosExtra,
                        asignadoPor = asignadoPorNombre,
                        asignadoEn = usuario.PermisosExtraFecha,
                        nota = usuario.PermisosExtraNota
                    }
                }
            });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante Login: {ErrorMessage}", ex.Message);
                return StatusCode(500, new 
                { 
                    message = "Error interno del servidor durante el login.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // ================================
        //       REFRESH TOKEN
        // POST: api/auth/refresh
        // ================================
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] DTOs.RefreshRequest req)
        {
            _logger.LogInformation("ðŸ”„ REFRESH ENDPOINT CALLED");
            _logger.LogInformation("   Total cookies received: {CookieCount}", Request.Cookies.Count);
            
            foreach (var cookie in Request.Cookies)
            {
                var display = cookie.Value.Length > 20 ? cookie.Value.Substring(0, 20) + "..." : cookie.Value;
                _logger.LogInformation("   ðŸ“¥ Cookie: {Key} = {Value}", cookie.Key, display);
            }

            // Permitir refresh usando cookie HttpOnly si no viene en body
            var incomingRt = req?.RefreshToken;
            _logger.LogInformation("   RefreshToken en body: {HasValue}", !string.IsNullOrEmpty(incomingRt));

            if (string.IsNullOrEmpty(incomingRt))
            {
                _logger.LogInformation("   Intentando leer refresh_token de cookies...");
                incomingRt = Request.Cookies["refresh_token"];
                _logger.LogInformation("   RefreshToken de cookie: {HasValue}", !string.IsNullOrEmpty(incomingRt));
            }

            if (string.IsNullOrEmpty(incomingRt))
            {
                _logger.LogWarning("âŒ refreshToken es requerido pero no se encontrÃ³ en body ni en cookies");
                return BadRequest(new { 
                    message = "refreshToken is required",
                    debug = new {
                        cookieCount = Request.Cookies.Count,
                        hasRefreshCookie = Request.Cookies.ContainsKey("refresh_token"),
                        hasBodyToken = req?.RefreshToken != null
                    }
                });
            }

            _logger.LogInformation("âœ… RefreshToken encontrado, buscando en DB...");

            var existing = await _context.RefreshTokens
                .Include(rt => rt.Usuario)
                .FirstOrDefaultAsync(rt => rt.Token == incomingRt);

            if (existing == null)
            {
                _logger.LogWarning("âŒ RefreshToken no encontrado en DB");
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }

            if (existing.Revoked)
            {
                _logger.LogWarning("âŒ RefreshToken revocado");
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }

            if (existing.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("âŒ RefreshToken expirado. ExpiresAt: {ExpiresAt}, Now: {Now}", existing.ExpiresAt, DateTime.UtcNow);
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }

            var user = existing.Usuario;
            if (user == null)
            {
                _logger.LogWarning("âŒ Usuario asociado al RefreshToken no encontrado");
                return Unauthorized(new { message = "Usuario no encontrado para este refresh token." });
            }

            _logger.LogInformation("âœ… ValidaciÃ³n exitosa. Generando nuevos tokens para usuarioId: {UserId}", user.Id);

            // create new tokens
            var (accessToken, newRefreshToken, expiresAt) = _tokenService.CreateTokens(user);

            // revoke existing
            existing.Revoked = true;

            // persist new refresh token
            var newRt = new RefreshToken
            {
                Token = newRefreshToken,
                UsuarioId = user.Id,
                ExpiresAt = expiresAt,
                Revoked = false
            };
            _context.RefreshTokens.Add(newRt);
            await _context.SaveChangesAsync();

            _logger.LogInformation("âœ… Nuevos tokens guardados. Actualizando cookies...");

            // Actualizar cookies
            Response.Cookies.Append("access_token", accessToken, BuildCookieOptions(expiresAt));
            Response.Cookies.Append("refresh_token", newRefreshToken, BuildCookieOptions(expiresAt.AddDays(7)));
            
            // Incluir datos mÃ­nimos del usuario para refrescar sesiÃ³n/Sidebar (incluye fotoPerfilUrl)
            return Ok(new 
            { 
                accessToken, 
                refreshToken = newRefreshToken,
                usuario = new {
                    user.Id,
                    user.Nombre,
                    user.Correo,
                    user.Rol,
                    fotoPerfilUrl = user.FotoPerfil,
                    NegocioId = user.NegocioId
                }
            });
        }

        // ================================
        //       LOGOUT (limpia cookies)
        // POST: api/auth/logout
        // ================================
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Expirar cookies
            var past = DateTime.UtcNow.AddDays(-1);
            var opts = BuildCookieOptions(past);
            Response.Cookies.Append("access_token", string.Empty, opts);
            Response.Cookies.Append("refresh_token", string.Empty, opts);
            return Ok(new { message = "SesiÃ³n cerrada" });
        }

        // ================================
        //       CREAR EMPLEADO
        // POST: api/auth/empleado
        // Solo DueÃ±o y Gerente pueden crear empleados
        // ================================
        [Authorize(Roles = "dueÃ±o,DueÃ±o,Dueno,gerente,Gerente")]
        [HttpPost("empleado")]
        public async Task<IActionResult> CrearEmpleado([FromBody] DTOs.CrearEmpleadoDTO empleadoDto)
        {
            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(empleadoDto.Nombre) ||
                string.IsNullOrWhiteSpace(empleadoDto.Apellido1) ||
                string.IsNullOrWhiteSpace(empleadoDto.Telefono))
            {
                return BadRequest(new { message = "Nombre, Apellido1 y Telefono son obligatorios." });
            }

            // Validar telÃ©fono (10 dÃ­gitos)
            if (empleadoDto.Telefono.Length != 10 || !empleadoDto.Telefono.All(char.IsDigit))
            {
                return BadRequest(new { message = "Telefono debe tener exactamente 10 dÃ­gitos numÃ©ricos." });
            }

            // Validar RFC si se proporciona
            if (!string.IsNullOrWhiteSpace(empleadoDto.RFC))
            {
                var rfcPattern = @"^[A-ZÃ‘&]{4}\d{6}[A-Z0-9]{3}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(empleadoDto.RFC.ToUpper(), rfcPattern))
                {
                    return BadRequest(new { message = "RFC invÃ¡lido. Debe tener el formato correcto (ej: PEPJ900101H02)." });
                }
            }

            // Obtener UsuarioId y NegocioId desde tenant (poblado por middleware)
            if (_tenant == null || !_tenant.UserId.HasValue || !_tenant.NegocioId.HasValue)
            {
                return Unauthorized(new { message = "No se pudo identificar al usuario o negocio en el token." });
            }

            var duenoId = _tenant.UserId.Value;
            var dueno = await _context.Usuarios.FindAsync(duenoId);
            if (dueno == null || dueno.NegocioId == null)
            {
                return BadRequest(new { message = "El dueÃ±o no tiene un negocio asignado." });
            }

            // Generar correo y contraseÃ±a automÃ¡ticos
            var correoBase = $"{empleadoDto.Apellido1.ToLower()}";
            if (!string.IsNullOrWhiteSpace(empleadoDto.Apellido2))
            {
                correoBase += $".{empleadoDto.Apellido2.ToLower()}";
            }
            
            // Asegurar que el correo sea Ãºnico
            var correo = $"{correoBase}@negocio{dueno.NegocioId}.local";
            int contador = 1;
            while (await _context.Usuarios.AnyAsync(u => u.Correo == correo))
            {
                correo = $"{correoBase}{contador}@negocio{dueno.NegocioId}.local";
                contador++;
            }

            // Generar contraseÃ±a aleatoria (8 caracteres)
            var password = GenerateRandomPassword(8);

            // Crear empleado
            var empleado = new Usuario
            {
                Nombre = empleadoDto.Nombre,
                Apellido1 = empleadoDto.Apellido1,
                Apellido2 = empleadoDto.Apellido2,
                Telefono = empleadoDto.Telefono,
                SueldoDiario = empleadoDto.SueldoDiario,
                RFC = empleadoDto.RFC?.ToUpper(),
                NumeroSeguroSocial = empleadoDto.NumeroSeguroSocial,
                Puesto = empleadoDto.Puesto,
                FechaIngreso = empleadoDto.FechaIngreso,
                Correo = correo,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Rol = "empleado",
                NegocioId = dueno.NegocioId,
                PrimerAcceso = true // OBLIGAR cambio de contraseÃ±a en primer login
            };

            _context.Usuarios.Add(empleado);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Empleado creado correctamente.",
                empleado = new
                {
                    empleado.Id,
                    empleado.Nombre,
                    empleado.Apellido1,
                    empleado.Apellido2,
                    empleado.Telefono,
                    empleado.SueldoDiario,
                    empleado.Correo,
                    empleado.Rol,
                    empleado.NegocioId
                },
                credenciales = new
                {
                    correo,
                    password // Devolver la contraseÃ±a generada al dueÃ±o (solo esta vez)
                }
            });
        }

        // ================================
        //       PERFIL
        // GET: api/auth/perfil
        // ================================
        [Authorize]
        [HttpGet("perfil")]
        public async Task<IActionResult> GetPerfil()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var usuario = await _context.Usuarios
                .Include(u => u.Negocio)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (usuario == null) return NotFound(new { message = "Usuario no encontrado." });

            return Ok(new
            {
                usuario.Id,
                usuario.Nombre,
                usuario.Correo,
                usuario.Rol,
                usuario.Telefono,
                usuario.Apellido1,
                usuario.Apellido2,
                usuario.SueldoDiario,
                usuario.RFC,
                usuario.NumeroSeguroSocial,
                usuario.Puesto,
                usuario.FechaIngreso,
                usuario.FotoPerfil,
                NegocioId = usuario.NegocioId,
                negocio = usuario.Negocio == null ? null : new
                {
                    usuario.Negocio.Id,
                    usuario.Negocio.NombreNegocio,
                    usuario.Negocio.OwnerId
                }
            });
        }

        // ================================
        //       BORRAR FOTO PERFIL
        // DELETE: api/auth/perfil/foto
        // ================================
        [Authorize]
        [HttpDelete("perfil/foto")]
        public async Task<IActionResult> DeleteFotoPerfil()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null) return NotFound(new { message = "Usuario no encontrado." });

            usuario.FotoPerfil = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Foto de perfil eliminada." });
        }

        // ================================
        //       PRIMER ACCESO - CAMBIO OBLIGATORIO DE CONTRASEÃ‘A
        // PUT: api/auth/primer-acceso
        // ================================
        [Authorize]
        [HttpPut("primer-acceso")]
        public async Task<IActionResult> CambiarPasswordPrimerAcceso([FromBody] DTOs.PrimerAccesoDTO dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null) return NotFound(new { message = "Usuario no encontrado." });

            // Validar que tenga primer acceso activo
            if (!usuario.PrimerAcceso)
            {
                return BadRequest(new { message = "Este usuario ya completÃ³ su primer acceso." });
            }

            // Validar nueva contraseÃ±a
            if (string.IsNullOrWhiteSpace(dto.NuevaPassword) || dto.NuevaPassword.Length < 6)
            {
                return BadRequest(new { message = "La nueva contraseÃ±a debe tener al menos 6 caracteres." });
            }

            // Actualizar contraseÃ±a y desactivar flag de primer acceso
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            usuario.PrimerAcceso = false;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "ContraseÃ±a actualizada. Ahora puedes acceder al sistema." });
        }

        private static string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}


