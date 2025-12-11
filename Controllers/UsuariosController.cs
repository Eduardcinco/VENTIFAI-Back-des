using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VentifyAPI.Data;
using VentifyAPI.DTOs;
using VentifyAPI.Models;
using VentifyAPI.Services;

namespace VentifyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ITenantContext _tenant;

        public UsuariosController(AppDbContext context, IWebHostEnvironment env, ITenantContext tenant)
        {
            _context = context;
            _env = env;
            _tenant = tenant;
        }

        private bool TryGetUserAndNegocio(out Usuario user, out int negocioId)
        {
            user = null!;
            negocioId = 0;
            if (_tenant == null || !_tenant.UserId.HasValue || !_tenant.NegocioId.HasValue) return false;
            var userId = _tenant.UserId.Value;
            user = _context.Usuarios.Find(userId)!;
            if (user == null) return false;
            negocioId = _tenant.NegocioId.Value;
            return true;
        }

        // GET: api/usuarios/empleados
        // Solo Dueño y Gerente pueden ver la lista de empleados
        [HttpGet("empleados")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> GetEmpleados()
        {
            if (!TryGetUserAndNegocio(out var requester, out var negocioId))
            {
                return Unauthorized(new { message = "Token inválido o negocio no asignado." });
            }

            // Devolver TODOS los usuarios del negocio (excepto el que hace la petición)
            var empleados = await _context.Usuarios
                .Where(u => u.NegocioId == negocioId && u.Id != requester.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Nombre,
                    u.Apellido1,
                    u.Apellido2,
                    u.Telefono,
                    u.SueldoDiario,
                    u.Correo,
                    u.Rol,
                    u.RFC,
                    u.NumeroSeguroSocial,
                    u.Puesto,
                    u.FechaIngreso,
                    u.FotoPerfil,
                    u.PermisosExtra,
                    u.PermisosExtraAsignadoPor,
                    u.PermisosExtraFecha,
                    u.PermisosExtraNota
                })
                .ToListAsync();

            // Parsear PermisosExtra de JSON string a lista
            var empleadosConPermisos = empleados.Select(e => new {
                e.Id,
                e.Nombre,
                e.Apellido1,
                e.Apellido2,
                e.Telefono,
                e.SueldoDiario,
                e.Correo,
                e.Rol,
                e.RFC,
                e.NumeroSeguroSocial,
                e.Puesto,
                e.FechaIngreso,
                e.FotoPerfil,
                permisosExtra = ParsePermisosExtra(e.PermisosExtra),
                e.PermisosExtraAsignadoPor,
                e.PermisosExtraFecha,
                e.PermisosExtraNota
            }).ToList();

            return Ok(new { empleados = empleadosConPermisos });
        }

        private static List<string> ParsePermisosExtra(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new List<string>();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        // PUT: api/usuarios/{id}
        // Solo Dueño y Gerente pueden editar empleados
        [HttpPut("{id:int}")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> UpdateEmpleado(int id, [FromBody] DTOs.CrearEmpleadoDTO dto)
        {
            if (!TryGetUserAndNegocio(out var requester, out var negocioId))
            {
                return Unauthorized(new { message = "Token inválido o negocio no asignado." });
            }

            var empleado = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.NegocioId == negocioId && u.Id != requester.Id);
            if (empleado == null)
            {
                return NotFound(new { message = "Usuario no encontrado en tu negocio." });
            }

            // No permitir editar al dueño original
            var negocio = await _context.Negocios.FindAsync(negocioId);
            if (negocio != null && negocio.OwnerId == id)
            {
                return BadRequest(new { message = "No se puede editar al dueño original del negocio desde aquí." });
            }

            if (!string.IsNullOrWhiteSpace(dto.Nombre)) empleado.Nombre = dto.Nombre;
            if (!string.IsNullOrWhiteSpace(dto.Apellido1)) empleado.Apellido1 = dto.Apellido1;
            empleado.Apellido2 = dto.Apellido2;
            if (!string.IsNullOrWhiteSpace(dto.Telefono))
            {
                if (dto.Telefono.Length != 10 || !dto.Telefono.All(char.IsDigit))
                {
                    return BadRequest(new { message = "Telefono debe tener exactamente 10 dígitos numéricos." });
                }
                empleado.Telefono = dto.Telefono;
            }
            empleado.SueldoDiario = dto.SueldoDiario;

            // Actualizar campos nuevos de Settings
            if (!string.IsNullOrWhiteSpace(dto.RFC))
            {
                var rfcPattern = @"^[A-ZÑ&]{4}\d{6}[A-Z0-9]{3}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.RFC.ToUpper(), rfcPattern))
                {
                    return BadRequest(new { message = "RFC inválido. Debe tener el formato correcto (ej: PEPJ900101H02)." });
                }
                empleado.RFC = dto.RFC.ToUpper();
            }
            else
            {
                empleado.RFC = null;
            }

            empleado.NumeroSeguroSocial = dto.NumeroSeguroSocial;
            empleado.Puesto = dto.Puesto;
            empleado.FechaIngreso = dto.FechaIngreso;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Empleado actualizado correctamente." });
        }

        // POST: api/usuarios/{id}/reset-password
        // Solo Dueño y Gerente pueden resetear contraseñas
        [HttpPost("{id:int}/reset-password")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            if (!TryGetUserAndNegocio(out var requester, out var negocioId))
            {
                return Unauthorized(new { message = "Token inválido o negocio no asignado." });
            }

            var empleado = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.NegocioId == negocioId && u.Rol == "empleado");
            if (empleado == null)
            {
                return NotFound(new { message = "Empleado no encontrado en tu negocio." });
            }

            var newPassword = GenerateRandomPassword(8);
            empleado.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Contraseña restablecida.", credenciales = new { correo = empleado.Correo, password = newPassword } });
        }

        // POST: api/usuarios/cambiar-password
        [HttpPost("cambiar-password")]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDTO dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            // Verificar contraseña actual
            if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.Password))
            {
                return BadRequest(new { message = "La contraseña actual es incorrecta." });
            }

            // Validar nueva contraseña
            if (string.IsNullOrWhiteSpace(dto.PasswordNueva) || dto.PasswordNueva.Length < 6)
            {
                return BadRequest(new { message = "La nueva contraseña debe tener al menos 6 caracteres." });
            }

            // Actualizar contraseña
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Contraseña actualizada correctamente." });
        }

        // PUT: api/usuarios/{id}/password
        // Solo Dueño y Gerente pueden cambiar password de otros usuarios
        [HttpPut("{id:int}/password")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> CambiarPasswordUsuario(int id, [FromBody] DTOs.CambiarPasswordUsuarioDTO dto)
        {
            if (!TryGetUserAndNegocio(out var requester, out var negocioId))
            {
                return Unauthorized(new { message = "Token inválido o negocio no asignado." });
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.NegocioId == negocioId);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado en tu negocio." });
            }

            if (string.IsNullOrWhiteSpace(dto?.PasswordNueva) || dto.PasswordNueva.Length < 6)
            {
                return BadRequest(new { message = "La nueva contraseña debe tener al menos 6 caracteres." });
            }

            usuario.Password = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Contraseña actualizada correctamente.", credenciales = new { correo = usuario.Correo, password = dto.PasswordNueva } });
        }

        // GET: api/usuarios/perfil
        [HttpGet("perfil")]
        public async Task<IActionResult> GetPerfil()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

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

            return Ok(new {
                id = usuario.Id,
                nombre = usuario.Nombre,
                apellido1 = usuario.Apellido1,
                apellido2 = usuario.Apellido2,
                correo = usuario.Correo,
                telefono = usuario.Telefono,
                rol = usuario.Rol,
                fotoPerfilUrl = usuario.FotoPerfil,
                permisosExtra = new {
                    modulos = modulosExtra,
                    asignadoPor = asignadoPorNombre,
                    asignadoEn = usuario.PermisosExtraFecha,
                    nota = usuario.PermisosExtraNota
                }
            });
        }

        // PUT: api/usuarios/mi-perfil
        // TODOS pueden actualizar su propio perfil (nombre, apellidos, teléfono)
        [HttpPut("mi-perfil")]
        public async Task<IActionResult> UpdateMiPerfil([FromBody] ActualizarMiPerfilDTO dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            // Actualizar campos si vienen en el DTO
            if (!string.IsNullOrWhiteSpace(dto.Nombre))
                usuario.Nombre = dto.Nombre;
            
            if (!string.IsNullOrWhiteSpace(dto.Apellido1))
                usuario.Apellido1 = dto.Apellido1;
            
            if (dto.Apellido2 != null) // Permitir vacío para borrar
                usuario.Apellido2 = string.IsNullOrWhiteSpace(dto.Apellido2) ? null : dto.Apellido2;
            
            if (!string.IsNullOrWhiteSpace(dto.Telefono))
            {
                if (dto.Telefono.Length != 10 || !dto.Telefono.All(char.IsDigit))
                {
                    return BadRequest(new { message = "Teléfono debe tener exactamente 10 dígitos numéricos." });
                }
                usuario.Telefono = dto.Telefono;
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = "Perfil actualizado correctamente.",
                usuario = new {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Apellido1,
                    usuario.Apellido2,
                    usuario.Correo,
                    usuario.Telefono,
                    usuario.Rol,
                    fotoPerfilUrl = usuario.FotoPerfil
                }
            });
        }

        // POST: api/usuarios/cambiar-correo
        [HttpPost("cambiar-correo")]
        public async Task<IActionResult> CambiarCorreo([FromBody] CambiarCorreoDTO dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            // Validar formato de email
            if (string.IsNullOrWhiteSpace(dto.NuevoCorreo) || !dto.NuevoCorreo.Contains("@"))
            {
                return BadRequest(new { message = "El correo electrónico no es válido." });
            }

            // Verificar que el correo no esté en uso
            var correoExiste = await _context.Usuarios.AnyAsync(u => u.Correo == dto.NuevoCorreo && u.Id != userId);
            if (correoExiste)
            {
                return BadRequest(new { message = "Este correo ya está registrado." });
            }

            // Actualizar correo
            usuario.Correo = dto.NuevoCorreo;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Correo actualizado correctamente." });
        }

        // DELETE: api/usuarios/foto-perfil
        [HttpDelete("foto-perfil")]
        public async Task<IActionResult> BorrarFotoPerfil()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            if (!string.IsNullOrWhiteSpace(usuario.FotoPerfil))
            {
                var relative = usuario.FotoPerfil.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);
                try
                {
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                catch { }
            }

            usuario.FotoPerfil = null;
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // POST: api/usuarios/foto-perfil
        [HttpPost("foto-perfil")]
        public async Task<IActionResult> SubirFotoPerfil(IFormFile file)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            // Validar que se envió un archivo
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No se recibió ningún archivo." });
            }

            // Validar tipo MIME
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest(new { message = "Solo se permiten imágenes JPG o PNG." });
            }

            // Validar tamaño (máximo 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "La imagen no puede superar 5MB." });
            }

            // Crear directorio si no existe
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "usuarios", userId.ToString());
            Directory.CreateDirectory(uploadsPath);

            // Generar nombre único
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"perfil_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Guardar archivo
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Actualizar URL en BD
            var fotoUrl = $"/uploads/usuarios/{userId}/{fileName}";
            usuario.FotoPerfil = fotoUrl;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, fotoUrl, message = "Foto de perfil actualizada correctamente." });
        }

        // DEVELOPMENT-ONLY: Reset password by correo (no auth) - only when ASPNETCORE_ENVIRONMENT=Development
        // POST: api/usuarios/dev/reset-password
        [AllowAnonymous]
        [HttpPost("dev/reset-password")]
        public async Task<IActionResult> DevResetPassword([FromBody] DTOs.DevResetPasswordDTO dto)
        {
            if (!_env.IsDevelopment())
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(dto?.Correo) || string.IsNullOrWhiteSpace(dto?.NuevaPassword))
            {
                return BadRequest(new { message = "Correo y NuevaPassword son requeridos." });
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == dto.Correo);
            if (usuario == null) return NotFound(new { message = "Usuario no encontrado." });

            if (dto.NuevaPassword.Length < 6) return BadRequest(new { message = "La nueva contraseña debe tener al menos 6 caracteres." });

            usuario.Password = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, correo = usuario.Correo, nuevaPassword = dto.NuevaPassword });
        }

        // POST: api/usuarios/cerrar-sesiones
        [HttpPost("cerrar-sesiones")]
        public async Task<IActionResult> CerrarSesiones([FromBody] CerrarSesionesDTO? dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            // Si se solicita mantener la sesión actual
            var mantenerActual = dto?.MantenerSesionActual ?? false;
            string? tokenActual = null;

            if (mantenerActual)
            {
                // Obtener el refresh token del encabezado o del body
                tokenActual = dto?.RefreshToken;
                
                // Si no viene en el body, intentar obtener del header Authorization
                if (string.IsNullOrEmpty(tokenActual))
                {
                    var authHeader = Request.Headers["Authorization"].ToString();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                    {
                        // En este caso, solo incrementamos TokenVersion pero no revocamos
                        // el refresh token asociado a esta sesión
                    }
                }
            }
            else
            {
                // Comportamiento original: cerrar todas las sesiones
                usuario.TokenVersion++;
                await _context.SaveChangesAsync();
            }

            // Revocar todos los refresh tokens excepto el actual (si se solicita mantenerlo)
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UsuarioId == userId && !rt.Revoked)
                .ToListAsync();

            var sesionesRevocadas = 0;
            foreach (var rt in refreshTokens)
            {
                // Si se debe mantener la sesión actual y este es el token actual, no revocarlo
                if (mantenerActual && !string.IsNullOrEmpty(tokenActual) && rt.Token == tokenActual)
                {
                    continue; // Saltar este token
                }
                
                rt.Revoked = true;
                sesionesRevocadas++;
            }
            
            await _context.SaveChangesAsync();

            var mensaje = mantenerActual 
                ? $"Se cerraron {sesionesRevocadas} sesiones. Sesión actual mantenida."
                : $"Se cerraron {sesionesRevocadas} sesiones activas.";

            return Ok(new { 
                success = true, 
                sesiones = sesionesRevocadas, 
                sesionActualMantenida = mantenerActual,
                message = mensaje 
            });
        }

        // PUT: api/usuarios/{id}/rol
        // Solo Dueño y Gerente pueden cambiar el rol de un usuario
        [HttpPut("{id:int}/rol")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> CambiarRol(int id, [FromBody] CambiarRolDTO dto)
        {
            if (!TryGetUserAndNegocio(out var requester, out var negocioId))
            {
                return Unauthorized(new { message = "Token inválido o negocio no asignado." });
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.NegocioId == negocioId);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado en tu negocio." });
            }

            // Validar que el rol sea válido
            var rolesValidos = new[] { "dueño", "Dueño", "Dueno", "gerente", "Gerente", "cajero", "Cajero", "almacenista", "Almacenista" };
            if (string.IsNullOrWhiteSpace(dto.Rol) || !rolesValidos.Contains(dto.Rol))
            {
                return BadRequest(new { message = "Rol inválido. Use: Dueño, Gerente, Cajero o Almacenista." });
            }

            // No permitir cambiar el rol del dueño original
            var negocio = await _context.Negocios.FindAsync(negocioId);
            if (negocio != null && negocio.OwnerId == id)
            {
                return BadRequest(new { message = "No se puede cambiar el rol del dueño original del negocio." });
            }

            // No permitir que un gerente o empleado cambie el rol de un usuario con rol "dueño"
            var requesterRol = requester.Rol?.ToLower();
            var usuarioRol = usuario.Rol?.ToLower();
            if (usuarioRol == "dueño" && requesterRol != "dueño")
            {
                return Forbid();
            }

            // Solo el dueño puede asignar rol de Gerente
            if (dto.Rol.ToLower() == "gerente" && requesterRol != "dueño")
            {
                return Forbid();
            }

            usuario.Rol = dto.Rol;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Rol actualizado correctamente.", usuario = new { usuario.Id, usuario.Nombre, usuario.Rol } });
        }

        // PUT: api/usuarios/{id}/permisos-extra
        // Solo Dueño y Gerente pueden asignar permisos extra
        [HttpPut("{id:int}/permisos-extra")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> AsignarPermisosExtra(int id, [FromBody] PermisosExtraDTO dto)
        {
            if (!TryGetUserAndNegocio(out var requester, out var negocioId))
            {
                return Unauthorized(new { message = "Token inválido o negocio no asignado." });
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.NegocioId == negocioId);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado en tu negocio." });
            }

            // No permitir asignar permisos extra al dueño original
            var negocio = await _context.Negocios.FindAsync(negocioId);
            if (negocio != null && negocio.OwnerId == id)
            {
                return BadRequest(new { message = "El dueño ya tiene todos los permisos." });
            }

            // Validar módulos
            var modulosValidos = new[] { "inventario", "pos", "caja", "reportes", "clientes" };
            var modulosLimpios = dto.Modulos?
                .Select(m => m.ToLower().Trim())
                .Where(m => modulosValidos.Contains(m))
                .Distinct()
                .ToList() ?? new List<string>();

            if (modulosLimpios.Count == 0)
            {
                // Si no hay módulos, limpiar permisos extra
                usuario.PermisosExtra = null;
                usuario.PermisosExtraAsignadoPor = null;
                usuario.PermisosExtraFecha = null;
                usuario.PermisosExtraNota = null;
            }
            else
            {
                usuario.PermisosExtra = System.Text.Json.JsonSerializer.Serialize(modulosLimpios);
                usuario.PermisosExtraAsignadoPor = requester.Id;
                usuario.PermisosExtraFecha = DateTime.UtcNow;
                usuario.PermisosExtraNota = dto.Nota;
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = modulosLimpios.Count > 0 
                    ? $"Permisos extra asignados: {string.Join(", ", modulosLimpios)}" 
                    : "Permisos extra eliminados.",
                usuario = new { 
                    usuario.Id, 
                    usuario.Nombre, 
                    usuario.Rol,
                    permisosExtra = modulosLimpios,
                    permisosExtraAsignadoPor = usuario.PermisosExtraAsignadoPor,
                    permisosExtraFecha = usuario.PermisosExtraFecha,
                    permisosExtraNota = usuario.PermisosExtraNota
                } 
            });
        }

        // DELETE: api/usuarios/{id}/permisos-extra
        // Solo Dueño y Gerente pueden quitar permisos extra
        [HttpDelete("{id:int}/permisos-extra")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> QuitarPermisosExtra(int id)
        {
            if (!TryGetUserAndNegocio(out var requester, out var negocioId))
            {
                return Unauthorized(new { message = "Token inválido o negocio no asignado." });
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.NegocioId == negocioId);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado en tu negocio." });
            }

            usuario.PermisosExtra = null;
            usuario.PermisosExtraAsignadoPor = null;
            usuario.PermisosExtraFecha = null;
            usuario.PermisosExtraNota = null;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Permisos extra eliminados." });
        }

        // GET: api/usuarios/{id}/permisos-extra
        // Solo Dueño y Gerente pueden ver permisos extra de otros
        [HttpGet("{id:int}/permisos-extra")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> GetPermisosExtra(int id)
        {
            if (!TryGetUserAndNegocio(out var requester, out var negocioId))
            {
                return Unauthorized(new { message = "Token inválido o negocio no asignado." });
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.NegocioId == negocioId);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado en tu negocio." });
            }

            var modulos = new List<string>();
            if (!string.IsNullOrEmpty(usuario.PermisosExtra))
            {
                try
                {
                    modulos = System.Text.Json.JsonSerializer.Deserialize<List<string>>(usuario.PermisosExtra) ?? new List<string>();
                }
                catch { }
            }

            string? asignadoPorNombre = null;
            if (usuario.PermisosExtraAsignadoPor.HasValue)
            {
                var asignador = await _context.Usuarios.FindAsync(usuario.PermisosExtraAsignadoPor.Value);
                asignadoPorNombre = asignador?.Nombre;
            }

            return Ok(new {
                modulos,
                asignadoPor = usuario.PermisosExtraAsignadoPor,
                asignadoPorNombre,
                fecha = usuario.PermisosExtraFecha,
                nota = usuario.PermisosExtraNota
            });
        }

        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
