using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentifyAPI.Data;
using VentifyAPI.Models;

namespace VentifyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VarianteProductoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VarianteProductoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/varianteproducto
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var variantes = await _context.VariantesProducto.Include(v => v.Producto).ToListAsync();
            return Ok(variantes);
        }

        // GET: api/varianteproducto/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var variante = await _context.VariantesProducto.Include(v => v.Producto).FirstOrDefaultAsync(v => v.Id == id);
            if (variante == null) return NotFound();
            return Ok(variante);
        }

        // POST: api/varianteproducto
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VarianteProducto variante)
        {
            _context.VariantesProducto.Add(variante);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = variante.Id }, variante);
        }

        // PUT: api/varianteproducto/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VarianteProducto variante)
        {
            if (id != variante.Id) return BadRequest();
            _context.Entry(variante).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/varianteproducto/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var variante = await _context.VariantesProducto.FindAsync(id);
            if (variante == null) return NotFound();
            _context.VariantesProducto.Remove(variante);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
