using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VentifyAPI.Data;
using VentifyAPI.DTOs;
using VentifyAPI.Services;

namespace VentifyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
    public class ReportesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ReporteExcelService _excelService;
        private readonly ReportePdfService _pdfService;
        private readonly ITenantContext _tenant;

        public ReportesController(AppDbContext context, ReporteExcelService excelService, ReportePdfService pdfService, ITenantContext tenant)
        {
            _context = context;
            _excelService = excelService;
            _pdfService = pdfService;
            _tenant = tenant;
        }

        private async Task<int> GetNegocioIdAsync()
        {
            if (_tenant.NegocioId.HasValue)
                return _tenant.NegocioId.Value;

            var userIdStr = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) throw new UnauthorizedAccessException();
            if (!int.TryParse(userIdStr, out var userId)) throw new UnauthorizedAccessException();
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario?.NegocioId == null)
                throw new UnauthorizedAccessException("Usuario sin negocio asignado");
            return usuario.NegocioId.Value;
        }

        /// <summary>
        /// GET: api/reportes/ventas?fechaInicio=2025-01-01&fechaFin=2025-01-31&tipoAgrupacion=dia
        /// </summary>
        [HttpGet("ventas")]
        public async Task<ActionResult<ReporteVentasCompletoDTO>> GetReporteVentas([FromQuery] FiltroReporteDTO filtro)
        {
            var negocioId = await GetNegocioIdAsync();
            var reporte = await GenerarReporteVentas(negocioId, filtro, incluirDetalle: false);
            return Ok(reporte);
        }

        /// <summary>
        /// POST: api/reportes/ventas/exportar
        /// Body: { "fechaInicio": "2025-01-01", "fechaFin": "2025-01-31", "tipoAgrupacion": "dia", "formato": "excel" }
        /// </summary>
        [HttpPost("ventas/exportar")]
        public async Task<IActionResult> ExportarReporteVentas([FromBody] FiltroReporteDTO filtro)
        {
            var negocioId = await GetNegocioIdAsync();
            var reporte = await GenerarReporteVentas(negocioId, filtro, incluirDetalle: true);

            var formato = filtro.Formato?.ToLower() ?? "excel";
            
            if (formato == "pdf")
            {
                var pdfBytes = _pdfService.GenerarReporteVentas(reporte);
                var fileName = $"Reporte_Ventas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            else
            {
                var excelBytes = _excelService.GenerarReporteVentas(reporte);
                var fileName = $"Reporte_Ventas_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        private async Task<ReporteVentasCompletoDTO> GenerarReporteVentas(int negocioId, FiltroReporteDTO filtro, bool incluirDetalle)
        {
            if (filtro.FechaInicio > filtro.FechaFin)
                throw new ArgumentException("La fecha de inicio no puede ser mayor a la fecha de fin");

            var negocio = await _context.Negocios.FindAsync(negocioId);
            var nombreNegocio = negocio?.NombreNegocio ?? "Negocio";

            // Rango inclusivo por día: [inicio 00:00, fin 24:00)
            // Si hay caja abierta, usar ventana desde última apertura hasta ahora (flujo real de POS)
            DateTime inicio;
            DateTime fin;
            var cajaAbierta = await _context.Cajas
                .Where(c => c.Abierta == true && c.NegocioId == negocioId)
                .OrderByDescending(c => c.FechaApertura)
                .FirstOrDefaultAsync();

            if (cajaAbierta != null)
            {
                inicio = cajaAbierta.FechaApertura;
                fin = DateTime.UtcNow;
            }
            else
            {
                // Rango inclusivo por día: [inicio 00:00, fin 24:00)
                inicio = filtro.FechaInicio.Date;
                fin = filtro.FechaFin.Date.AddDays(1);
            }

            // Tu modelo: Venta { FechaHora, TotalPagado, FormaPago, UsuarioId }
            var ventasQuery = _context.Ventas.AsNoTracking()
                .Where(v => v.NegocioId == negocioId &&
                           v.FechaHora >= inicio &&
                           v.FechaHora < fin);

            if (!string.IsNullOrEmpty(filtro.MetodoPago))
                ventasQuery = ventasQuery.Where(v => v.FormaPago != null && v.FormaPago.ToLower() == filtro.MetodoPago!.ToLower());

            if (filtro.CajerosIds != null && filtro.CajerosIds.Any())
                ventasQuery = ventasQuery.Where(v => filtro.CajerosIds.Contains(v.UsuarioId));

            var ventas = await ventasQuery.ToListAsync();

            // Calcular totales (tu modelo no tiene IVA ni descuento separados, solo TotalPagado)
            var resumenGeneral = new ReporteVentasAgregadoDTO
            {
                Periodo = "Resumen General",
                FechaInicio = filtro.FechaInicio,
                FechaFin = filtro.FechaFin,
                TotalVentas = ventas.Count,
                TotalIngresos = ventas.Sum(v => v.TotalPagado),
                TotalSubtotal = ventas.Sum(v => v.TotalPagado / 1.16m), // Estimado sin IVA
                TotalIva = ventas.Sum(v => v.TotalPagado - (v.TotalPagado / 1.16m)), // Estimado IVA
                TotalDescuentos = 0, // Tu modelo no tiene descuentos
                TicketPromedio = ventas.Any() ? ventas.Average(v => v.TotalPagado) : 0,
                VentaMaxima = ventas.Any() ? ventas.Max(v => v.TotalPagado) : 0,
                VentaMinima = ventas.Any() ? ventas.Min(v => v.TotalPagado) : 0,
                CajerosActivos = ventas.Select(v => v.UsuarioId).Distinct().Count(),
                TotalEfectivo = ventas.Where(v => (v.FormaPago ?? string.Empty).ToLower() == "efectivo").Sum(v => v.TotalPagado),
                TotalTarjeta = ventas.Where(v => (v.FormaPago ?? string.Empty).ToLower() == "tarjeta").Sum(v => v.TotalPagado),
                TotalTransferencia = ventas.Where(v => (v.FormaPago ?? string.Empty).ToLower() == "transferencia").Sum(v => v.TotalPagado),
                VentasEfectivo = ventas.Count(v => (v.FormaPago ?? string.Empty).ToLower() == "efectivo"),
                VentasTarjeta = ventas.Count(v => (v.FormaPago ?? string.Empty).ToLower() == "tarjeta"),
                VentasTransferencia = ventas.Count(v => (v.FormaPago ?? string.Empty).ToLower() == "transferencia")
            };

            var datosPorPeriodo = GenerarDatosPorPeriodo(ventas, filtro.TipoAgrupacion);
            var topProductos = await ObtenerTopProductos(negocioId, filtro);
            
            List<ReporteVentaDetalleDTO>? ventasDetalladas = null;
            if (incluirDetalle)
                ventasDetalladas = await ObtenerVentasDetalladas(ventas.Select(v => v.Id).ToList());

            return new ReporteVentasCompletoDTO
            {
                NombreNegocio = nombreNegocio,
                FechaGeneracion = DateTime.Now,
                TipoReporte = $"Ventas por {filtro.TipoAgrupacion}",
                FechaInicio = inicio,
                FechaFin = fin,
                ModoCajaAbierta = cajaAbierta != null,
                InicioReal = inicio,
                FinReal = fin,
                ResumenGeneral = resumenGeneral,
                DatosPorPeriodo = datosPorPeriodo,
                TopProductos = topProductos,
                VentasDetalladas = ventasDetalladas
            };
        }

        private List<ReporteVentasAgregadoDTO> GenerarDatosPorPeriodo(List<Models.Venta> ventas, string tipoAgrupacion)
        {
            IEnumerable<IGrouping<string, Models.Venta>> grupos;

            var t = string.IsNullOrWhiteSpace(tipoAgrupacion) ? "dia" : tipoAgrupacion.ToLower();
            switch (t)
            {
                case "dia":
                    grupos = ventas.GroupBy(v => v.FechaHora.Date.ToString("yyyy-MM-dd"));
                    break;
                case "semana":
                    grupos = ventas.GroupBy(v => $"{v.FechaHora.Year}-S{GetWeekOfYear(v.FechaHora)}");
                    break;
                case "mes":
                    grupos = ventas.GroupBy(v => v.FechaHora.ToString("yyyy-MM"));
                    break;
                case "anio":
                    grupos = ventas.GroupBy(v => v.FechaHora.Year.ToString());
                    break;
                default:
                    grupos = ventas.GroupBy(v => v.FechaHora.Date.ToString("yyyy-MM-dd"));
                    break;
            }

            return grupos.Select(g => new ReporteVentasAgregadoDTO
            {
                Periodo = g.Key,
                FechaInicio = g.Min(v => v.FechaHora),
                FechaFin = g.Max(v => v.FechaHora),
                TotalVentas = g.Count(),
                TotalIngresos = g.Sum(v => v.TotalPagado),
                TotalSubtotal = g.Sum(v => v.TotalPagado / 1.16m),
                TotalIva = g.Sum(v => v.TotalPagado - (v.TotalPagado / 1.16m)),
                TotalDescuentos = 0,
                TicketPromedio = g.Any() ? g.Average(v => v.TotalPagado) : 0,
                VentaMaxima = g.Max(v => v.TotalPagado),
                VentaMinima = g.Min(v => v.TotalPagado),
                CajerosActivos = g.Select(v => v.UsuarioId).Distinct().Count(),
                // Clientes eliminados: no contabilizar clientes
                TotalEfectivo = g.Where(v => (v.FormaPago ?? string.Empty).ToLower() == "efectivo").Sum(v => v.TotalPagado),
                TotalTarjeta = g.Where(v => (v.FormaPago ?? string.Empty).ToLower() == "tarjeta").Sum(v => v.TotalPagado),
                TotalTransferencia = g.Where(v => (v.FormaPago ?? string.Empty).ToLower() == "transferencia").Sum(v => v.TotalPagado),
                VentasEfectivo = g.Count(v => (v.FormaPago ?? string.Empty).ToLower() == "efectivo"),
                VentasTarjeta = g.Count(v => (v.FormaPago ?? string.Empty).ToLower() == "tarjeta"),
                VentasTransferencia = g.Count(v => (v.FormaPago ?? string.Empty).ToLower() == "transferencia")
            }).OrderBy(d => d.Periodo).ToList();
        }

        private async Task<List<ProductoMasVendidoDTO>> ObtenerTopProductos(int negocioId, FiltroReporteDTO filtro)
        {
            // Tu modelo: DetalleVenta { VentaId, ProductoId, Cantidad, PrecioUnitario, Subtotal }
            var query = from dv in _context.DetallesVenta
                        join v in _context.Ventas on dv.VentaId equals v.Id
                        join p in _context.Productos on dv.ProductoId equals p.Id
                        where v.NegocioId == negocioId &&
                              v.FechaHora >= filtro.FechaInicio &&
                              v.FechaHora <= filtro.FechaFin
                        group new { dv, v } by new { p.Id, p.Nombre, p.CodigoBarras, CategoriaId = p.CategoryId } into g
                        select new
                        {
                            g.Key.Id,
                            g.Key.Nombre,
                            g.Key.CodigoBarras,
                            g.Key.CategoriaId,
                            CantidadVendida = g.Sum(x => x.dv.Cantidad),
                            TotalVentas = g.Sum(x => x.dv.Subtotal),
                            NumeroTransacciones = g.Select(x => x.v.Id).Distinct().Count(),
                            PrecioPromedio = g.Average(x => x.dv.PrecioUnitario)
                        };

            var productos = await query.OrderByDescending(p => p.CantidadVendida).Take(20).ToListAsync();

            // Obtener nombres de categorías
            var categoriaIds = productos.Where(p => p.CategoriaId.HasValue).Select(p => p.CategoriaId!.Value).Distinct().ToList();
            var categorias = await _context.Categories.Where(c => categoriaIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name);

            return productos.Select(p => new ProductoMasVendidoDTO
            {
                ProductoId = p.Id,
                ProductoNombre = p.Nombre,
                CodigoBarras = p.CodigoBarras,
                CategoriaNombre = p.CategoriaId.HasValue && categorias.ContainsKey(p.CategoriaId.Value) ? categorias[p.CategoriaId.Value] : null,
                CantidadVendida = p.CantidadVendida,
                TotalVentas = p.TotalVentas,
                NumeroTransacciones = p.NumeroTransacciones,
                PrecioPromedio = p.PrecioPromedio
            }).ToList();
        }

        private async Task<List<ReporteVentaDetalleDTO>> ObtenerVentasDetalladas(List<int> ventasIds)
        {
            var ventas = await _context.Ventas
                .Include(v => v.Usuario) // Cajero
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(v => ventasIds.Contains(v.Id))
                .ToListAsync();

            return ventas.Select(v => new ReporteVentaDetalleDTO
            {
                VentaId = v.Id,
                Folio = v.Ticket ?? $"V-{v.Id}",
                FechaVenta = v.FechaHora,
                Total = v.TotalPagado,
                Subtotal = v.TotalPagado / 1.16m,
                Iva = v.TotalPagado - (v.TotalPagado / 1.16m),
                DescuentoAplicado = 0,
                MetodoPago = v.FormaPago,
                TipoVenta = "local",
                NombreCajero = v.Usuario?.Nombre ?? "N/A",
                // Cliente eliminado
                NombreCliente = null,
                TelefonoCliente = null,
                CantidadProductos = v.Detalles?.Sum(d => d.Cantidad) ?? 0,
                Productos = v.Detalles?.Select(d => new ReporteProductoVendidoDTO
                {
                    ProductoNombre = d.Producto?.Nombre ?? "N/A",
                    CodigoBarras = d.Producto?.CodigoBarras,
                    CategoriaNombre = null, // Puedes agregar si lo necesitas
                    VarianteNombre = null,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    SubtotalDetalle = d.Subtotal,
                    DescuentoDetalle = 0
                }).ToList() ?? new()
            }).OrderByDescending(v => v.FechaVenta).ToList();
        }

        private int GetWeekOfYear(DateTime date)
        {
            return System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }
    }
}
