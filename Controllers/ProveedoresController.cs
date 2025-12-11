using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VentifyAPI.Data;
using VentifyAPI.Models;

namespace VentifyAPI.Controllers
{
    [Route("api/proveedores")]
    [ApiController]
    [Authorize]
    public class ProveedoresController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProveedoresController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId);
            var negocioId = user?.NegocioId;
            var query = _context.Proveedores.AsQueryable();
            if (negocioId != null) query = query.Where(p => p.NegocioId == negocioId);
            var list = await query.ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Proveedores.FindAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Proveedor dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre)) return BadRequest(new { message = "Nombre requerido." });
            var userIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId);
            dto.NegocioId = user?.NegocioId;
            _context.Proveedores.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Proveedor dto)
        {
            if (id != dto.Id) return BadRequest();
            var existing = await _context.Proveedores.FindAsync(id);
            if (existing == null) return NotFound();
            existing.Nombre = dto.Nombre;
            existing.Correo = dto.Correo;
            existing.Telefono = dto.Telefono;
            existing.Direccion = dto.Direccion;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _context.Proveedores.FindAsync(id);
            if (existing == null) return NotFound();
            _context.Proveedores.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
