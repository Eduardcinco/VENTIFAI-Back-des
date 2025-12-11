using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VentifyAPI.Data;
using VentifyAPI.Models;

namespace VentifyAPI.Controllers
{
    [ApiController]
    [Route("api/categorias")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CategoriesController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search = null)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var negocioId = await _db.Usuarios.Where(u => u.Id == userId).Select(u => u.NegocioId).FirstOrDefaultAsync();
            var query = _db.Categories
                .Include(c => c.Usuario)
                .Where(c => c.Usuario != null && c.Usuario.NegocioId == negocioId);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Name.Contains(search));
            // Ordenar por nombre para una mejor UX en el frontend
            query = query.OrderBy(c => c.Name);
            var categories = await query.ToListAsync();
            return Ok(categories.Select(c => new { c.Id, c.Name, c.ParentId }));
        }

        // GET: api/categories/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var negocioId = await _db.Usuarios.Where(u => u.Id == userId).Select(u => u.NegocioId).FirstOrDefaultAsync();
            var cat = await _db.Categories.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == id && c.Usuario != null && c.Usuario.NegocioId == negocioId);
            if (cat == null) return NotFound(new { message = "Category not found." });
            return Ok(new { cat.Id, cat.Name, cat.ParentId });
        }

        // POST: api/categories
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Category dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var cat = new Category { Name = dto.Name, ParentId = dto.ParentId, UsuarioId = userId };
            _db.Categories.Add(cat);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = cat.Id }, new { cat.Id, cat.Name, cat.ParentId });
        }

        // PUT: api/categories/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Category dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == userId);
            if (cat == null) return NotFound();
            cat.Name = dto.Name;
            cat.ParentId = dto.ParentId;
            await _db.SaveChangesAsync();
            return Ok(new { cat.Id, cat.Name, cat.ParentId });
        }

        // DELETE: api/categories/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == userId);
            if (cat == null) return NotFound();

            // Validar si tiene hijos
            var hasChildren = await _db.Categories.AnyAsync(c => c.ParentId == id && c.UsuarioId == userId);
            if (hasChildren)
            {
                return Conflict(new { message = "No se puede eliminar la categoría porque tiene subcategorías. Elimine o reasigne las subcategorías primero." });
            }

            _db.Categories.Remove(cat);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
