using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VentifyAPI.Data;
using VentifyAPI.Models;
using VentifyAPI.Services;

namespace VentifyAPI.Controllers
{
    [Route("api/ventas")]
    [ApiController]
        [Authorize]
    public class VentaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PdfService _pdfService;
        private readonly TicketService _ticketService;

        public VentaController(AppDbContext context, PdfService pdfService, TicketService ticketService)
        {
            _context = context;
            _pdfService = pdfService;
            _ticketService = ticketService;
        }

        // GET: api/ventas/mis-ventas?fecha=YYYY-MM-DD
        // Para cajeros: solo sus ventas del día
        [HttpGet("mis-ventas")]
        public async Task<IActionResult> GetMisVentas([FromQuery] string? fecha = null)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var negocioId = await _context.Usuarios.Where(u => u.Id == userId).Select(u => u.NegocioId).FirstOrDefaultAsync();

            // Fecha por defecto: hoy
            var targetDate = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(fecha) && DateTime.TryParse(fecha, out var parsedDate))
                targetDate = parsedDate.Date;

            var ventas = await _context.Ventas
                .Include(v => v.Detalles)
                .Where(v => v.NegocioId == negocioId && v.UsuarioId == userId)
                .Where(v => v.FechaHora.Date == targetDate)
                .OrderByDescending(v => v.FechaHora)
                .Select(v => new {
                    v.Id,
                    fecha = v.FechaHora,
                    total = v.TotalPagado,
                    v.FormaPago,
                    items = v.Detalles.Count
                })
                .ToListAsync();

            return Ok(ventas);
        }

        // GET: api/ventas?from=YYYY-MM-DD&to=YYYY-MM-DD
        // Solo Dueño y Gerente pueden ver todas las ventas
        [HttpGet]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> GetAll([FromQuery] string? from = null, [FromQuery] string? to = null)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var negocioId = await _context.Usuarios.Where(u => u.Id == userId).Select(u => u.NegocioId).FirstOrDefaultAsync();

            var query = _context.Ventas.Include(v => v.Detalles).Where(v => v.NegocioId == negocioId);
            if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var fromDate))
                query = query.Where(v => v.FechaHora >= fromDate);
            if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var toDate))
                query = query.Where(v => v.FechaHora <= toDate.AddDays(1));

            var ventas = await query.OrderByDescending(v => v.FechaHora).ToListAsync();
            var totalVentas = ventas.Count;
            var totalMonto = ventas.Sum(v => v.TotalPagado);
            return Ok(new { ventas, totalVentas, totalMonto });
        }

        // GET: api/ventas/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var negocioId = await _context.Usuarios.Where(u => u.Id == userId).Select(u => u.NegocioId).FirstOrDefaultAsync();

            var venta = await _context.Ventas.Include(v => v.Detalles).FirstOrDefaultAsync(v => v.Id == id && v.NegocioId == negocioId);
            if (venta == null) return NotFound();
            return Ok(venta);
        }

        // GET: api/ventas/{id}/pdf
        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> GetVentaPdf(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var negocioId = await _context.Usuarios.Where(u => u.Id == userId).Select(u => u.NegocioId).FirstOrDefaultAsync();

            try
            {
                var pdfBytes = _pdfService.GenerateVentaPdf(id, negocioId.Value);
                return File(pdfBytes, "application/pdf", $"ticket-venta-{id}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/ventas
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DTOs.VentaCreateDTO dto)
        {
            if (dto == null || dto.Items == null || dto.Items.Count == 0)
                return BadRequest(new { message = "Items requeridos para crear una venta." });

            // get user id from token
            var userIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            // Validate that there is an open caja for this negocio
            var negocioId = (await _context.Usuarios.Where(u => u.Id == userId).Select(u => u.NegocioId).FirstOrDefaultAsync()) ?? 0;
            var cajaAbierta = await _context.Cajas
                .Where(c => c.Abierta == true && c.NegocioId == negocioId)
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();
            if (cajaAbierta == null)
                return BadRequest(new { message = "No hay caja abierta para este negocio. Debe abrir una caja antes de realizar ventas." });

            // Validate efectivo payment has monto
            if (dto.PaymentMethod?.ToLower() == "efectivo" && (dto.MontoRecibido == null || dto.MontoRecibido <= 0))
                return BadRequest(new { message = "Para pago en efectivo debe indicar monto recibido." });

            // basic validation and stock update
            foreach (var it in dto.Items)
            {
                var prod = await _context.Productos.FirstOrDefaultAsync(p => p.Id == it.ProductoId);
                if (prod == null) return BadRequest(new { message = $"Producto {it.ProductoId} no encontrado." });
                if (!prod.Activo) return BadRequest(new { message = $"Producto {prod.Nombre} no está activo." });
                if (prod.StockActual < it.Cantidad) return BadRequest(new { message = $"Stock insuficiente para producto {prod.Nombre}. Disponible: {prod.StockActual}" });
            }

            var venta = new Venta
            {
                UsuarioId = userId,
                TotalPagado = dto.Total,
                FormaPago = dto.PaymentMethod ?? "Efectivo",
                MontoRecibido = dto.MontoRecibido,
                Cambio = dto.Cambio,
                NegocioId = negocioId
            };

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync(); // to get venta.Id

            // Update caja montoActual
            cajaAbierta.MontoActual += venta.TotalPagado;
            
            // Registrar movimiento de entrada por venta
            var movimientoVenta = new MovimientoCaja
            {
                CajaId = cajaAbierta.Id,
                NegocioId = negocioId,
                UsuarioId = userId,
                Tipo = "entrada",
                Monto = venta.TotalPagado,
                Categoria = "Venta",
                Descripcion = $"Venta #{venta.Id}",
                MetodoPago = venta.FormaPago,
                FechaHora = DateTime.UtcNow,
                SaldoDespues = cajaAbierta.MontoActual,
                Referencia = $"VENTA-{venta.Id}"
            };
            _context.MovimientosCaja.Add(movimientoVenta);

            foreach (var it in dto.Items)
            {
                var prod = await _context.Productos.FirstAsync(p => p.Id == it.ProductoId);
                // decrement stock
                prod.StockActual -= it.Cantidad;
                var det = new DetalleVenta
                {
                    VentaId = venta.Id,
                    ProductoId = it.ProductoId,
                    VarianteProductoId = it.VarianteProductoId,
                    Cantidad = it.Cantidad,
                    PrecioUnitario = it.Precio,
                    Subtotal = it.Precio * it.Cantidad
                };
                _context.DetallesVenta.Add(det);
            }

            await _context.SaveChangesAsync();

            // Generar ticket de texto y guardar en la venta
            try
            {
                var ticketText = await _ticketService.GenerateTicketTextAsync(venta.Id, negocioId);
                venta.Ticket = ticketText;
                await _context.SaveChangesAsync();
            }
            catch { /* no bloquear la venta por error de ticket */ }

            return CreatedAtAction(nameof(GetById), new { id = venta.Id }, new { venta.Id, ticket = venta.Ticket });
        }

        // GET: api/ventas/{id}/ticket?format=html|text
        [HttpGet("{id}/ticket")]
        public async Task<IActionResult> GetTicket(int id, [FromQuery] string format = "text")
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var negocioId = await _context.Usuarios.Where(u => u.Id == userId).Select(u => u.NegocioId).FirstOrDefaultAsync() ?? 0;

            if (format.Equals("html", StringComparison.OrdinalIgnoreCase))
            {
                var html = await _ticketService.GenerateTicketHtmlAsync(id, negocioId);
                return Content(html, "text/html; charset=utf-8");
            }
            else
            {
                var text = await _ticketService.GenerateTicketTextAsync(id, negocioId);
                return Content(text, "text/plain; charset=utf-8");
            }
        }

        // PUT: api/venta/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Venta venta)
        {
            if (id != venta.Id) return BadRequest();
            _context.Entry(venta).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/ventas/{id}
        // Solo Dueño y Gerente pueden eliminar ventas
        [HttpDelete("{id}")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var negocioId = await _context.Usuarios.Where(u => u.Id == userId).Select(u => u.NegocioId).FirstOrDefaultAsync();

            var venta = await _context.Ventas
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.Id == id && v.NegocioId == negocioId);
            if (venta == null) return NotFound();

            // Restaurar stock de productos
            foreach (var detalle in venta.Detalles)
            {
                if (detalle.Producto != null)
                {
                    detalle.Producto.StockActual += detalle.Cantidad;
                }
            }

            // Actualizar caja si hay una abierta (restar el monto de la venta)
            var cajaAbierta = await _context.Cajas
                .Where(c => c.Abierta == true && c.NegocioId == negocioId)
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();

            if (cajaAbierta != null)
            {
                cajaAbierta.MontoActual -= venta.TotalPagado;

                // Registrar movimiento de salida por cancelación de venta
                var movimientoCancelacion = new MovimientoCaja
                {
                    CajaId = cajaAbierta.Id,
                    NegocioId = negocioId.Value,
                    UsuarioId = userId,
                    Tipo = "salida",
                    Monto = venta.TotalPagado,
                    Categoria = "Cancelación de Venta",
                    Descripcion = $"Cancelación de venta #{venta.Id}",
                    MetodoPago = venta.FormaPago,
                    FechaHora = DateTime.UtcNow,
                    SaldoDespues = cajaAbierta.MontoActual,
                    Referencia = $"CANCEL-VENTA-{venta.Id}"
                };
                _context.MovimientosCaja.Add(movimientoCancelacion);
            }

            _context.Ventas.Remove(venta);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
