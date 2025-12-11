using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentifyAPI.Data;
using VentifyAPI.Models;

namespace VentifyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PuntoDeVentaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PuntoDeVentaController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/puntodeventa
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var puntos = await _context.PuntosDeVenta.ToListAsync();
            return Ok(puntos);
        }

        // GET: api/puntodeventa/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var punto = await _context.PuntosDeVenta.FindAsync(id);
            if (punto == null) return NotFound();
            return Ok(punto);
        }

        // POST: api/puntodeventa
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PuntoDeVenta punto)
        {
            _context.PuntosDeVenta.Add(punto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = punto.Id }, punto);
        }

        // PUT: api/puntodeventa/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PuntoDeVenta punto)
        {
            if (id != punto.Id) return BadRequest();
            _context.Entry(punto).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/puntodeventa/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var punto = await _context.PuntosDeVenta.FindAsync(id);
            if (punto == null) return NotFound();
            _context.PuntosDeVenta.Remove(punto);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
