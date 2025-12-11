using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VentifyAPI.Data;
using VentifyAPI.Models;
using VentifyAPI.DTOs;

namespace VentifyAPI.Controllers
{
    [Route("api/caja")]
    [ApiController]
    [Authorize]
    public class CajaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Services.ITenantContext _tenant;

        public CajaController(AppDbContext context, Services.ITenantContext tenant)
        {
            _context = context;
            _tenant = tenant;
        }

        // GET: api/caja/current
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var negocioId = await _context.Usuarios.Where(u => u.Id == userId).Select(u => u.NegocioId).FirstOrDefaultAsync();
            var caja = await _context.Cajas
                .Where(c => c.Abierta == true && c.NegocioId == negocioId)
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();
            if (caja == null)
                return Ok(new { abierta = false, caja = (object?)null });
            return Ok(new { abierta = true, caja });
        }

        // POST: api/caja/open
        [HttpPost("open")]
        public async Task<IActionResult> Open([FromBody] Caja dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var nombreUsuario = User.FindFirstValue(ClaimTypes.Name);

            var negocioIdNullable = await _context.Usuarios
                .Where(u => u.Id == userId)
                .Select(u => u.NegocioId)
                .FirstOrDefaultAsync();
            if (negocioIdNullable == null)
            {
                return BadRequest(new { message = "Usuario no tiene negocio asignado." });
            }
            var negocioId = negocioIdNullable.Value;

            // Verificar si ya hay una caja abierta en este negocio
            var existente = await _context.Cajas
                .Where(c => c.NegocioId == negocioId && c.Abierta)
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();
            if (existente != null)
            {
                // Tolerante: devolver el estado actual sin crear una nueva
                return Ok(new { abierta = true, caja = existente });
            }

            var caja = new Caja
            {
                // Siempre tomar NegocioId desde el usuario autenticado
                NegocioId = negocioId,
                UsuarioAperturaId = userId,
                FechaApertura = DateTime.UtcNow,
                MontoInicial = dto.MontoInicial,
                MontoActual = dto.MontoInicial,
                Abierta = true,
                AbiertaPor = nombreUsuario,
                Turno = string.IsNullOrWhiteSpace(dto.Turno) ? "General" : dto.Turno!.Trim()
            };
            _context.Cajas.Add(caja);
            await _context.SaveChangesAsync();
            return Ok(new { abierta = true, caja });
        }

        // POST: api/caja/close
        [HttpPost("close")]
        public async Task<IActionResult> Close([FromBody] Caja dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            // Cerrar caja únicamente dentro del mismo negocio del usuario
            var negocioIdNullable = await _context.Usuarios
                .Where(u => u.Id == userId)
                .Select(u => u.NegocioId)
                .FirstOrDefaultAsync();
            if (negocioIdNullable == null)
            {
                return BadRequest(new { message = "Usuario no tiene negocio asignado." });
            }
            var negocioId = negocioIdNullable.Value;

            var caja = await _context.Cajas
                .FirstOrDefaultAsync(c => c.Id == dto.Id && c.Abierta && c.NegocioId == negocioId);
            if (caja == null) return NotFound(new { message = "Caja no encontrada o ya cerrada." });

            // Validar monto de cierre
            var montoCierre = dto.MontoCierre ?? caja.MontoActual;
            if (montoCierre < 0)
                return BadRequest(new { message = "El monto de cierre no puede ser negativo." });

            // Generar resumen automático de ventas del día (filtrar por negocio y rango de fecha)
            var ventasDelDia = await _context.Ventas
                .Where(v => v.NegocioId == negocioId && v.FechaHora >= caja.FechaApertura && v.FechaHora <= DateTime.UtcNow)
                .ToListAsync();
            var totalVentas = ventasDelDia.Sum(v => v.TotalPagado);
            var numeroVentas = ventasDelDia.Count;
            var resumenAuto = $"Ventas: {numeroVentas} | Total: ${totalVentas:F2} | Esperado: ${caja.MontoInicial + totalVentas:F2} | Real: ${montoCierre:F2}";
            var resumenFinal = string.IsNullOrWhiteSpace(dto.ResumenCierre) ? resumenAuto : dto.ResumenCierre + " | " + resumenAuto;

            caja.MontoActual = montoCierre;
            caja.MontoCierre = montoCierre;
            caja.ResumenCierre = resumenFinal;
            caja.FechaCierre = DateTime.UtcNow;
            caja.UsuarioCierreId = userId;  // Auditoría: quién cerró
            caja.Abierta = false;
            await _context.SaveChangesAsync();
            return Ok(new { abierta = false, caja });
        }

        // POST: api/caja/movimiento -> registrar entrada/salida de dinero
        [HttpPost("movimiento")]
        public async Task<IActionResult> RegistrarMovimiento([FromBody] MovimientoCajaDTO dto)
        {
            if (!_tenant.UserId.HasValue) return Unauthorized();
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var userId = _tenant.UserId.Value;
            var negocioId = _tenant.NegocioId.Value;

            // Validar que haya una caja abierta
            var caja = await _context.Cajas
                .Where(c => c.NegocioId == negocioId && c.Abierta)
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();

            if (caja == null)
                return BadRequest(new { message = "No hay una caja abierta. Abre una caja primero." });

            // Validaciones
            if (dto.Monto <= 0)
                return BadRequest(new { message = "El monto debe ser mayor a 0." });

            if (dto.Tipo != "entrada" && dto.Tipo != "salida")
                return BadRequest(new { message = "El tipo debe ser 'entrada' o 'salida'." });

            // Calcular nuevo saldo
            var nuevoSaldo = dto.Tipo == "entrada" 
                ? caja.MontoActual + dto.Monto 
                : caja.MontoActual - dto.Monto;

            if (nuevoSaldo < 0)
                return BadRequest(new { message = "Saldo insuficiente en caja. No se puede retirar más de lo disponible." });

            // Crear movimiento
            var movimiento = new MovimientoCaja
            {
                CajaId = caja.Id,
                NegocioId = negocioId,
                UsuarioId = userId,
                Tipo = dto.Tipo,
                Monto = dto.Monto,
                Categoria = dto.Categoria,
                Descripcion = dto.Descripcion,
                MetodoPago = dto.MetodoPago ?? "Efectivo",
                FechaHora = DateTime.UtcNow,
                SaldoDespues = nuevoSaldo,
                Referencia = dto.Referencia
            };

            _context.MovimientosCaja.Add(movimiento);

            // Actualizar saldo de caja
            caja.MontoActual = nuevoSaldo;
            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = $"{(dto.Tipo == "entrada" ? "Ingreso" : "Egreso")} registrado exitosamente.",
                movimiento = new {
                    movimiento.Id,
                    movimiento.Tipo,
                    movimiento.Monto,
                    movimiento.Categoria,
                    movimiento.Descripcion,
                    movimiento.FechaHora,
                    movimiento.SaldoDespues
                },
                saldoActual = caja.MontoActual
            });
        }

        // GET: api/caja/movimientos -> obtener movimientos de la caja actual
        [HttpGet("movimientos")]
        public async Task<IActionResult> GetMovimientos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] string? tipo)
        {
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var negocioId = _tenant.NegocioId.Value;

            // Obtener caja actual
            var caja = await _context.Cajas
                .Where(c => c.NegocioId == negocioId && c.Abierta)
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();

            if (caja == null)
                return BadRequest(new { message = "No hay una caja abierta." });

            var query = _context.MovimientosCaja
                .Include(m => m.Usuario)
                .Where(m => m.CajaId == caja.Id);

            // Filtros opcionales
            if (desde.HasValue)
                query = query.Where(m => m.FechaHora >= desde.Value);
            if (hasta.HasValue)
                query = query.Where(m => m.FechaHora <= hasta.Value);
            if (!string.IsNullOrEmpty(tipo))
                query = query.Where(m => m.Tipo == tipo);

            var movimientos = await query
                .OrderByDescending(m => m.FechaHora)
                .Select(m => new {
                    m.Id,
                    m.Tipo,
                    m.Monto,
                    m.Categoria,
                    m.Descripcion,
                    m.MetodoPago,
                    m.FechaHora,
                    m.SaldoDespues,
                    m.Referencia,
                    usuario = m.Usuario != null ? m.Usuario.Nombre : "N/A"
                })
                .ToListAsync();

            return Ok(new {
                cajaId = caja.Id,
                saldoActual = caja.MontoActual,
                movimientos
            });
        }

        // GET: api/caja/resumen -> resumen de movimientos por categoría
        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen()
        {
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var negocioId = _tenant.NegocioId.Value;

            var caja = await _context.Cajas
                .Where(c => c.NegocioId == negocioId && c.Abierta)
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();

            if (caja == null)
                return BadRequest(new { message = "No hay una caja abierta." });

            var movimientos = await _context.MovimientosCaja
                .Where(m => m.CajaId == caja.Id)
                .ToListAsync();

            var totalEntradas = movimientos.Where(m => m.Tipo == "entrada").Sum(m => m.Monto);
            var totalSalidas = movimientos.Where(m => m.Tipo == "salida").Sum(m => m.Monto);
            
            var entradaPorCategoria = movimientos
                .Where(m => m.Tipo == "entrada")
                .GroupBy(m => m.Categoria)
                .Select(g => new { categoria = g.Key, total = g.Sum(m => m.Monto), cantidad = g.Count() })
                .OrderByDescending(x => x.total)
                .ToList();

            var salidaPorCategoria = movimientos
                .Where(m => m.Tipo == "salida")
                .GroupBy(m => m.Categoria)
                .Select(g => new { categoria = g.Key, total = g.Sum(m => m.Monto), cantidad = g.Count() })
                .OrderByDescending(x => x.total)
                .ToList();

            return Ok(new {
                cajaId = caja.Id,
                montoInicial = caja.MontoInicial,
                saldoActual = caja.MontoActual,
                totalEntradas,
                totalSalidas,
                gananciaReal = (caja.MontoActual - caja.MontoInicial),
                entradaPorCategoria,
                salidaPorCategoria
            });
        }
    }
}
