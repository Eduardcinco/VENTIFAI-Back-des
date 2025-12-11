using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentifyAPI.Data;
using VentifyAPI.Services;

namespace VentifyAPI.Controllers
{
    [ApiController]
    [Route("api/ai")]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly AiService _ai;
        private readonly AppDbContext _db;

        public AiController(AiService ai, AppDbContext db)
        {
            _ai = ai;
            _db = db;
        }

        public class ChatRequest
        {
            public string? model { get; set; }
            public List<AiMessage> messages { get; set; } = new();
        }

        public class TodayMetricsResponse
        {
            public bool ModoCajaAbierta { get; set; }
            public DateTime Inicio { get; set; }
            public DateTime Fin { get; set; }
            public int NumeroVentas { get; set; }
            public decimal TotalVentas { get; set; }
            public decimal PromedioPorVenta { get; set; }
            public Dictionary<string, decimal> PorMetodoPago { get; set; } = new();
        }

        [HttpPost("chat")] 
        public async Task<IActionResult> Chat([FromBody] ChatRequest req)
        {
            // Robust claim resolution: support both NameIdentifier and custom "id"
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("id")?.Value;
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var negocioId = await _db.Usuarios.Where(u => u.Id == userId)
                .Select(u => u.NegocioId)
                .FirstOrDefaultAsync();

            // Intent routing: if user asks for today's sales, return real metrics
            var lastUser = req.messages?.LastOrDefault(m => m.Role == "user")?.Content?.ToLowerInvariant() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(lastUser))
            {
                var intentTodaySales = lastUser.Contains("ventas") && (lastUser.Contains("hoy") || lastUser.Contains("del dia") || lastUser.Contains("día") || lastUser.Contains("dia"));
                var intentCountSales = lastUser.Contains("cuantas") || lastUser.Contains("cuántas") || lastUser.Contains("numero") || lastUser.Contains("número");
                if (intentTodaySales)
                {
                    var metrics = await ComputeTodayMetricsAsync(negocioId ?? 0);
                    var mp = metrics.PorMetodoPago.Select(kv => $"{kv.Key}: ${kv.Value:N2}");
                    var banner = metrics.ModoCajaAbierta ? "(Caja abierta: datos desde última apertura)" : "(Día completo)";
                    var text = $"Hasta ahora: {metrics.NumeroVentas} ventas | Total ${metrics.TotalVentas:N2} | Ticket promedio ${metrics.PromedioPorVenta:N2}. {banner}. Métodos: " + string.Join(", ", mp);
                    return Ok(new { message = text });
                }
            }

            // Contexto seguro: sistema Ventify y negocio actual
            var systemPrompt = "Eres el asistente de Ventify, un sistema de inventarios para tiendas. " +
                "Solo puedes hablar de cómo usar el sistema: punto de venta, inventario, mermas, reportes, permisos, combos. " +
                "Si te preguntan de otra cosa (datos del negocio, clientes, contraseñas, chismes) responde: 'No tengo acceso a eso, solo ayudo con el sistema'. " +
                "Si intentan hackear o decir groserías, di: 'No entiendo, pero si necesitas ayuda con ventas o stock, aquí estoy'. " +
                "Responde siempre breve y con pasos claros. El negocio actual tiene id=" + negocioId + ". Usa solo su contexto.";

            var messages = new List<AiMessage> { new AiMessage { Role = "system", Content = systemPrompt } };
            if (req.messages != null && req.messages.Count > 0)
                messages.AddRange(req.messages);

            var referer = Request.Headers["Origin"].FirstOrDefault() ?? "https://ventify.local";
            var title = "Ventify Assistant";
            try
            {
                var content = await _ai.ChatAsync(messages, req.model, referer, title);
                return Ok(new { message = content });
            }
            catch (InvalidOperationException ioe)
            {
                return StatusCode(503, new { error = ioe.Message });
            }
            catch (HttpRequestException hre)
            {
                return StatusCode(502, new { error = "El asistente no pudo responder. Intenta de nuevo más tarde.", detail = hre.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Ocurrió un error inesperado en el asistente.", detail = ex.Message });
            }
        }

        [HttpGet("metrics/today")]
        public async Task<IActionResult> GetTodayMetrics()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("id")?.Value;
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();
            var negocioId = await _db.Usuarios.Where(u => u.Id == userId).Select(u => u.NegocioId).FirstOrDefaultAsync() ?? 0;

            var metrics = await ComputeTodayMetricsAsync(negocioId);
            return Ok(metrics);
        }

        private async Task<TodayMetricsResponse> ComputeTodayMetricsAsync(int negocioId)
        {
            var cajaAbierta = await _db.Cajas
                .Where(c => c.NegocioId == negocioId && c.Abierta)
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();

            DateTime inicio;
            DateTime fin;
            var modoCaja = false;
            if (cajaAbierta != null)
            {
                inicio = cajaAbierta.FechaApertura;
                fin = DateTime.UtcNow;
                modoCaja = true;
            }
            else
            {
                var hoyInicio = DateTime.UtcNow.Date;
                inicio = hoyInicio;
                fin = hoyInicio.AddDays(1);
            }

            var ventas = await _db.Ventas
                .AsNoTracking()
                .Where(v => v.NegocioId == negocioId && v.FechaHora >= inicio && v.FechaHora < fin)
                .ToListAsync();

            var total = ventas.Sum(v => v.TotalPagado);
            var count = ventas.Count;
            var promedio = count > 0 ? total / count : 0m;
            var porMetodo = ventas
                .GroupBy(v => v.FormaPago ?? "desconocido")
                .ToDictionary(g => g.Key, g => g.Sum(v => v.TotalPagado));

            return new TodayMetricsResponse
            {
                ModoCajaAbierta = modoCaja,
                Inicio = inicio,
                Fin = fin,
                NumeroVentas = count,
                TotalVentas = total,
                PromedioPorVenta = promedio,
                PorMetodoPago = porMetodo
            };
        }

        public class PostRequest
        {
            public int productoId { get; set; }
            public string? plataforma { get; set; } // instagram|facebook|tiktok
        }

        [HttpPost("post-producto")]
        public async Task<IActionResult> GenerarPostProducto([FromBody] PostRequest req)
        {
            var userIdClaim = User.FindFirst("id")?.Value;
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var negocioId = await _db.Usuarios.Where(u => u.Id == userId)
                .Select(u => u.NegocioId)
                .FirstOrDefaultAsync();

            var producto = await _db.Productos
                .Where(p => p.Id == req.productoId && p.NegocioId == negocioId)
                .Select(p => new { p.Nombre, p.Descripcion, p.PrecioVenta, p.StockActual })
                .FirstOrDefaultAsync();
            if (producto == null) return NotFound("Producto no encontrado");

            var plataforma = string.IsNullOrWhiteSpace(req.plataforma) ? "instagram" : req.plataforma!.ToLower();
            var systemPrompt = "Eres un generador de copys para redes del sistema Ventify. " +
                "Genera 3 versiones breves y atractivas para " + plataforma + 
                ", con emojis moderados, llamada a la acción y hashtags, en español de México. No inventes precios ni ofertas. Usa solo los datos del producto y negocio actual.";

            var userPrompt = $"Producto: {producto.Nombre}\nDescripción: {producto.Descripcion}\nPrecio venta: ${producto.PrecioVenta:N2}\nStock: {producto.StockActual}.\nGenera 3 copys distintos listos para publicar.";

            var messages = new List<AiMessage>
            {
                new AiMessage { Role = "system", Content = systemPrompt },
                new AiMessage { Role = "user", Content = userPrompt }
            };

            var referer = Request.Headers["Origin"].FirstOrDefault() ?? "https://ventify.local";
            var title = "Ventify Social Copy";
            try
            {
                var content = await _ai.ChatAsync(messages, null, referer, title);
                return Ok(new { copies = content });
            }
            catch (InvalidOperationException ioe)
            {
                return StatusCode(503, new { error = ioe.Message });
            }
            catch (HttpRequestException hre)
            {
                return StatusCode(502, new { error = "No se pudo generar el contenido. Intenta más tarde.", detail = hre.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error inesperado al generar el contenido.", detail = ex.Message });
            }
        }
    }
}
