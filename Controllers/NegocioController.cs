using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VentifyAPI.Data;
using VentifyAPI.DTOs;
using VentifyAPI.Models;

namespace VentifyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NegocioController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NegocioController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/negocio/perfil
        [HttpGet("perfil")]
        [Authorize]
        public async Task<IActionResult> GetPerfil()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var negocioId = await _context.Usuarios
                .Where(u => u.Id == userId)
                .Select(u => u.NegocioId)
                .FirstOrDefaultAsync();

            if (negocioId == null)
                return NotFound(new { message = "Usuario no tiene negocio asignado." });

            var negocio = await _context.Negocios.FindAsync(negocioId);
            if (negocio == null)
                return NotFound(new { message = "Negocio no encontrado." });

            var dto = new NegocioPerfilDTO
            {
                Id = negocio.Id,
                NombreNegocio = negocio.NombreNegocio,
                Direccion = negocio.Direccion,
                Telefono = negocio.Telefono,
                Correo = negocio.Correo,
                RFC = negocio.RFC,
                GiroComercial = negocio.GiroComercial
            };

            return Ok(dto);
        }

        // PUT: api/negocio/perfil
        // Solo el Dueño puede cambiar el nombre del negocio
        // El Gerente puede actualizar otros campos pero NO el nombre
        [HttpPut("perfil")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> UpdatePerfil([FromBody] NegocioPerfilDTO dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
                return Unauthorized();
            
            var negocioId = usuario.NegocioId;
            if (negocioId == null)
                return NotFound(new { message = "Usuario no tiene negocio asignado." });

            var negocio = await _context.Negocios.FindAsync(negocioId);
            if (negocio == null)
                return NotFound(new { message = "Negocio no encontrado." });

            // Solo el dueño puede cambiar el nombre del negocio
            var esDueno = usuario.Rol?.ToLower() == "dueño" || usuario.Rol?.ToLower() == "dueno";
            if (!esDueno && dto.NombreNegocio != negocio.NombreNegocio)
            {
                return Forbid(); // Gerente intentó cambiar el nombre
            }

            // Actualizar campos
            if (esDueno)
            {
                negocio.NombreNegocio = dto.NombreNegocio;
            }
            negocio.Direccion = dto.Direccion;
            negocio.Telefono = dto.Telefono;
            negocio.Correo = dto.Correo;
            negocio.RFC = dto.RFC;
            negocio.GiroComercial = dto.GiroComercial;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Perfil del negocio actualizado correctamente." });
        }

        // GET: api/negocio/branding
        [HttpGet("branding")]
        [Authorize]
        public async Task<IActionResult> GetBranding()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var negocioId = await _context.Usuarios
                .Where(u => u.Id == userId)
                .Select(u => u.NegocioId)
                .FirstOrDefaultAsync();

            if (negocioId == null)
                return NotFound(new { message = "Usuario no tiene negocio asignado." });

            var negocio = await _context.Negocios.FindAsync(negocioId);
            if (negocio == null)
                return NotFound(new { message = "Negocio no encontrado." });

            var branding = new DTOs.BrandingDTO
            {
                ColorPrimario = negocio.ColorPrimario,
                ColorSecundario = negocio.ColorSecundario,
                ColorFondo = negocio.ColorFondo,
                ColorAcento = negocio.ColorAcento,
                ModoOscuro = negocio.ModoOscuro
            };

            return Ok(branding);
        }

        // POST: api/negocio/branding
        // Dueño y Gerente pueden actualizar branding
        [HttpPost("branding")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> UpdateBranding([FromBody] DTOs.BrandingDTO dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var negocioId = await _context.Usuarios
                .Where(u => u.Id == userId)
                .Select(u => u.NegocioId)
                .FirstOrDefaultAsync();

            if (negocioId == null)
                return NotFound(new { message = "Usuario no tiene negocio asignado." });

            var negocio = await _context.Negocios.FindAsync(negocioId);
            if (negocio == null)
                return NotFound(new { message = "Negocio no encontrado." });

            // Actualizar branding
            negocio.ColorPrimario = dto.ColorPrimario;
            negocio.ColorSecundario = dto.ColorSecundario;
            negocio.ColorFondo = dto.ColorFondo;
            negocio.ColorAcento = dto.ColorAcento;
            negocio.ModoOscuro = dto.ModoOscuro;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Branding actualizado correctamente." });
        }

        // GET: api/negocio
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? nombre)
        {
            var query = _context.Negocios.AsQueryable();
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                query = query.Where(n => n.NombreNegocio.Contains(nombre));
            }
            var negocios = await query.ToListAsync();
            return Ok(negocios);
        }

        // GET: api/negocio/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var negocio = await _context.Negocios.FindAsync(id);
            if (negocio == null) return NotFound();
            return Ok(negocio);
        }

        // POST: api/negocio
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Negocio negocio)
        {
            if (string.IsNullOrWhiteSpace(negocio.NombreNegocio))
                return BadRequest(new { message = "El nombre del negocio es obligatorio." });

            // Puedes agregar más validaciones aquí (unicidad, etc.)

            _context.Negocios.Add(negocio);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = negocio.Id }, negocio);
        }

        // PUT: api/negocio/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Negocio negocio)
        {
            if (id != negocio.Id) return BadRequest(new { message = "El id de la URL no coincide con el del objeto." });
            if (string.IsNullOrWhiteSpace(negocio.NombreNegocio))
                return BadRequest(new { message = "El nombre del negocio es obligatorio." });

            var existe = await _context.Negocios.AnyAsync(n => n.Id == id);
            if (!existe) return NotFound(new { message = "Negocio no encontrado." });

            _context.Entry(negocio).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/negocio/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var negocio = await _context.Negocios.FindAsync(id);
            if (negocio == null) return NotFound(new { message = "Negocio no encontrado." });
            _context.Negocios.Remove(negocio);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
