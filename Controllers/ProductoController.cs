using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VentifyAPI.Data;
using VentifyAPI.Models;
using VentifyAPI.Services;

namespace VentifyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PdfService _pdfService;
        private readonly ITenantContext _tenant;

        public ProductoController(AppDbContext context, PdfService pdfService, ITenantContext tenant)
        {
            _context = context;
            _pdfService = pdfService;
            _tenant = tenant;
        }

        // POST: api/producto/{id}/reabastecer  -> reabastecer stock con nueva compra
        // Solo Dueño, Gerente y Almacenista pueden reabastecer
        [HttpPost("{id}/reabastecer")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente,almacenista,Almacenista")]
        public async Task<IActionResult> ReabastecerProducto(int id, [FromBody] DTOs.ReabastecerProductoDTO dto)
        {
            if (!_tenant.UserId.HasValue) return Unauthorized();
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var userId = _tenant.UserId.Value;
            var negocioId = _tenant.NegocioId.Value;

            // Buscar el producto
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == negocioId);
                
            if (producto == null)
                return NotFound(new { message = "Producto no encontrado en tu negocio." });
            
            // Validaciones
            var fieldErrors = new Dictionary<string, string>();
            
            if (dto.CantidadComprada <= 0)
                fieldErrors["cantidadComprada"] = "La cantidad debe ser mayor a 0.";
                
            if (dto.PrecioCompra.HasValue && dto.PrecioCompra <= 0)
                fieldErrors["precioCompra"] = "El precio de compra debe ser mayor a 0.";
                
            if (dto.PrecioVenta.HasValue && dto.PrecioVenta <= 0)
                fieldErrors["precioVenta"] = "El precio de venta debe ser mayor a 0.";

            // Validar que precio venta > precio compra
            var precioCompraFinal = dto.PrecioCompra ?? producto.PrecioCompra;
            var precioVentaFinal = dto.PrecioVenta ?? producto.PrecioVenta;
            if (precioVentaFinal <= precioCompraFinal)
                fieldErrors["precioVenta"] = "El precio de venta debe ser mayor al precio de compra.";
                
            if (dto.Merma < 0 || dto.Merma > dto.CantidadComprada)
                fieldErrors["merma"] = "La merma no puede ser negativa ni mayor a la cantidad comprada.";

            if (dto.StockMinimo.HasValue && dto.StockMinimo < 0)
                fieldErrors["stockMinimo"] = "El stock mínimo no puede ser negativo.";

            if (fieldErrors.Count > 0)
                return BadRequest(new { title = "Validation Failed", fieldErrors });

            // Guardar valores anteriores para auditoría
            var stockAntes = producto.StockActual;
            var mermaAntes = producto.Merma;
            var precioCompraAntes = producto.PrecioCompra;
            var precioVentaAntes = producto.PrecioVenta;
            
            // Actualizar precios si se proporcionan
            if (dto.PrecioCompra.HasValue)
                producto.PrecioCompra = dto.PrecioCompra.Value;
                
            if (dto.PrecioVenta.HasValue)
                producto.PrecioVenta = dto.PrecioVenta.Value;
            
            // Calcular cantidad neta a agregar (comprada - merma)
            var mermaReabastecimiento = dto.Merma ?? 0;
            var cantidadNeta = dto.CantidadComprada - mermaReabastecimiento;
            
            // Actualizar stock
            producto.StockActual += cantidadNeta;
            
            // Actualizar merma acumulada del producto
            producto.Merma += mermaReabastecimiento;
            
            // Actualizar cantidad inicial (representa el total que ha entrado)
            producto.CantidadInicial += dto.CantidadComprada;
            
            // Actualizar stock mínimo si se proporciona
            if (dto.StockMinimo.HasValue)
                producto.StockMinimo = dto.StockMinimo.Value;
            
            // Registrar evento de reabastecimiento (si existe la tabla)
            try
            {
                var evento = new MermaEvento
                {
                    ProductoId = producto.Id,
                    Cantidad = -cantidadNeta, // Negativo porque es entrada (no salida como merma)
                    Motivo = $"Reabastecimiento: +{dto.CantidadComprada} unidades compradas" + 
                             (mermaReabastecimiento > 0 ? $", -{mermaReabastecimiento} merma" : ""),
                    UsuarioId = userId,
                    NegocioId = negocioId,
                    FechaUtc = DateTime.UtcNow,
                    StockAntes = stockAntes,
                    StockDespues = producto.StockActual,
                    MermaAntes = mermaAntes,
                    MermaDespues = producto.Merma
                };
                _context.MermaEventos.Add(evento);
            }
            catch { }
            
            await _context.SaveChangesAsync();
            
            return Ok(new { 
                message = "Producto reabastecido exitosamente",
                stockNuevo = producto.StockActual,
                mermaNueva = producto.Merma,
                cantidadAgregada = cantidadNeta,
                producto = new {
                    producto.Id,
                    producto.Nombre,
                    producto.StockActual,
                    producto.Merma,
                    producto.PrecioCompra,
                    producto.PrecioVenta,
                    producto.StockMinimo
                }
            });
        }

        // POST: api/producto/{id}/merma  -> agrega merma con auditoría
        // Solo Dueño, Gerente y Almacenista pueden registrar merma
        [HttpPost("{id}/merma")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente,almacenista,Almacenista")]
        public async Task<IActionResult> AddMerma(int id, [FromBody] DTOs.MermaRequestDTO body)
        {
            if (!_tenant.UserId.HasValue) return Unauthorized();
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var userId = _tenant.UserId.Value;
            var negocioId = _tenant.NegocioId.Value;

            // Aceptar 'incremento' o alias 'cantidad'
            var incremento = body.Incremento.HasValue ? body.Incremento : body.Cantidad;
            var motivo = body.Motivo;

            var fieldErrors = new Dictionary<string, string>();
            if (incremento == null)
                fieldErrors["incremento"] = "Campo requerido (incremento o cantidad).";
            else if (incremento <= 0)
                fieldErrors["incremento"] = "Debe ser mayor a 0.";
            if (fieldErrors.Count > 0)
                return BadRequest(new { message = "Errores de validación", fieldErrors });

            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == negocioId);
            if (producto == null) return NotFound(new { message = "Producto no encontrado en tu negocio." });

            // Validaciones de negocio
            if (incremento > producto.StockActual)
                return BadRequest(new { message = "La merma no puede exceder el stock actual." });
            // En este punto incremento fue validado (no null y > 0)
            var inc = incremento!.Value;
            var nuevaMerma = producto.Merma + inc;
            if (nuevaMerma > producto.CantidadInicial)
                return BadRequest(new { message = "La merma acumulada no puede exceder la cantidad inicial." });

            var stockAntes = producto.StockActual;
            var mermaAntes = producto.Merma;

            producto.Merma = nuevaMerma;
            producto.StockActual = producto.StockActual - inc;

            try
            {
                var evento = new MermaEvento
                {
                    ProductoId = producto.Id,
                    Cantidad = inc,
                    Motivo = string.IsNullOrWhiteSpace(motivo) ? "Sin motivo" : motivo!.Trim(),
                    UsuarioId = userId,
                    NegocioId = negocioId,
                    FechaUtc = DateTime.UtcNow,
                    StockAntes = stockAntes,
                    StockDespues = producto.StockActual,
                    MermaAntes = mermaAntes,
                    MermaDespues = producto.Merma
                };
                _context.MermaEventos.Add(evento);
            }
            catch { }

            await _context.SaveChangesAsync();
            return Ok(new { producto.Id, producto.Merma, producto.StockActual });
        }

        // GET: api/producto/stock-bajo
        [HttpGet("stock-bajo")]
        public async Task<IActionResult> GetStockBajo()
        {
            if (!_tenant.UserId.HasValue) return Unauthorized();
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var negocioId = _tenant.NegocioId.Value;
            var productos = await _context.Productos
                .Include(p => p.Category)
                .Where(p => p.NegocioId == negocioId && p.Activo)
                .Where(p => p.StockActual <= p.StockMinimo)
                .OrderBy(p => p.StockActual)
                .ToListAsync();

            var result = productos.Select(p => new {
                p.Id,
                p.Nombre,
                p.StockActual,
                p.StockMinimo,
                category = p.Category == null ? null : new { id = p.Category.Id, name = p.Category.Name }
            });
            return Ok(result);
        }

        // GET: api/producto
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? nombre, [FromQuery] string? categoria, [FromQuery] string? codigoBarras, [FromQuery] int? id, [FromQuery] bool includeInactive = false, [FromQuery] string? filtro = null)
        {
            if (!_tenant.UserId.HasValue) return Unauthorized();
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var negocioId = _tenant.NegocioId.Value;
            var query = _context.Productos
                .Include(p => p.Variantes)
                .Include(p => p.Category)
                .Where(p => p.NegocioId == negocioId)
                .AsQueryable();
            
            // Soportar filtro=activos|todos (nuevo frontend) o includeInactive (legacy)
            var showAll = includeInactive || string.Equals(filtro, "todos", StringComparison.OrdinalIgnoreCase);
            if (!showAll)
                query = query.Where(p => p.Activo == true);
            if (!string.IsNullOrWhiteSpace(nombre))
                query = query.Where(p => p.Nombre.Contains(nombre));
            if (!string.IsNullOrWhiteSpace(categoria))
                query = query.Where(p => p.Categoria == categoria);
            if (!string.IsNullOrWhiteSpace(codigoBarras))
                query = query.Where(p => p.CodigoBarras == codigoBarras);
            if (id.HasValue)
                query = query.Where(p => p.Id == id.Value);
            var productos = await query.ToListAsync();
            var result = productos.Select(p => {
                var precioConDescuento = Services.DescuentoService.CalcularPrecioConDescuento(p);
                var descuentoActivo = Services.DescuentoService.DescuentoEstaActivo(p);
                var ahorro = Services.DescuentoService.CalcularAhorro(p);
                
                return new {
                    p.Id,
                    p.Nombre,
                    p.Descripcion,
                    p.PrecioCompra,
                    p.PrecioVenta,
                    precioOriginal = p.PrecioVenta,
                    precioFinal = precioConDescuento,
                    tieneDescuento = descuentoActivo,
                    descuentoPorcentaje = descuentoActivo ? p.DescuentoPorcentaje : null,
                    ahorro = descuentoActivo ? ahorro : 0,
                    p.CantidadInicial,
                    p.Merma,
                    p.StockActual,
                    stock = p.StockActual,
                    p.StockMinimo,
                    p.UnidadMedida,
                    p.CodigoBarras,
                    p.ImagenUrl,
                    p.Activo,
                    p.CategoryId,
                    category = p.Category == null ? null : new { id = p.Category.Id, name = p.Category.Name },
                    variantes = p.Variantes.Select(v => new { v.Id, v.Nombre, v.Precio, v.Stock }).ToList()
                };
            });
            return Ok(result);
        }

        // GET: api/producto/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!_tenant.UserId.HasValue) return Unauthorized();
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var negocioId = _tenant.NegocioId.Value;
            var producto = await _context.Productos
                .Include(p => p.Variantes)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == negocioId);
            if (producto == null) return NotFound();
            
            var precioConDescuento = Services.DescuentoService.CalcularPrecioConDescuento(producto);
            var descuentoActivo = Services.DescuentoService.DescuentoEstaActivo(producto);
            var ahorro = Services.DescuentoService.CalcularAhorro(producto);
            
            var dto = new {
                producto.Id,
                producto.Nombre,
                producto.Descripcion,
                producto.PrecioCompra,
                producto.PrecioVenta,
                precioOriginal = producto.PrecioVenta,
                precioFinal = precioConDescuento,
                tieneDescuento = descuentoActivo,
                descuentoPorcentaje = descuentoActivo ? producto.DescuentoPorcentaje : null,
                ahorro = descuentoActivo ? ahorro : 0,
                producto.CantidadInicial,
                producto.Merma,
                producto.StockActual,
                stock = producto.StockActual,
                producto.StockMinimo,
                producto.UnidadMedida,
                producto.CodigoBarras,
                producto.ImagenUrl,
                producto.Activo,
                producto.CategoryId,
                category = producto.Category == null ? null : new { id = producto.Category.Id, name = producto.Category.Name },
                variantes = producto.Variantes.Select(v => new { v.Id, v.Nombre, v.Precio, v.Stock }).ToList(),
                // Info adicional del descuento para administración
                descuentoInfo = descuentoActivo ? new {
                    porcentaje = producto.DescuentoPorcentaje,
                    fechaInicio = producto.DescuentoFechaInicio,
                    fechaFin = producto.DescuentoFechaFin,
                    horaInicio = producto.DescuentoHoraInicio,
                    horaFin = producto.DescuentoHoraFin
                } : null
            };
            return Ok(dto);
        }

        // POST: api/producto
        // Solo Dueño, Gerente y Almacenista pueden crear productos
        [HttpPost]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente,almacenista,Almacenista")]
        public async Task<IActionResult> Create([FromBody] Producto producto)
        {
            var fieldErrors = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(producto.Nombre))
                fieldErrors["nombre"] = "El nombre del producto es obligatorio.";
            if (producto.PrecioVenta <= 0)
                fieldErrors["precioVenta"] = "El precio de venta debe ser mayor a 0.";
            if (producto.PrecioCompra < 0)
                fieldErrors["precioCompra"] = "El precio de compra no puede ser negativo.";
            // Validación de negocio: precio venta > precio compra
            if (producto.PrecioVenta > 0 && producto.PrecioCompra > 0 && producto.PrecioVenta <= producto.PrecioCompra)
                fieldErrors["precioVenta"] = "El precio de venta debe ser mayor al precio de compra para tener ganancia.";
            if (producto.CantidadInicial < 0)
                fieldErrors["cantidadInicial"] = "La cantidad inicial no puede ser negativa.";
            if (producto.Merma < 0)
                fieldErrors["merma"] = "La merma no puede ser negativa.";
            // Validación: merma no puede exceder cantidad inicial
            if (producto.CantidadInicial >= 0 && producto.Merma > producto.CantidadInicial)
                fieldErrors["merma"] = $"La merma ({producto.Merma}) no puede ser mayor a la cantidad inicial ({producto.CantidadInicial}).";
            if (producto.StockActual < 0)
                fieldErrors["stockActual"] = "El stock actual no puede ser negativo.";
            if (producto.StockMinimo < 0)
                fieldErrors["stockMinimo"] = "El stock mínimo no puede ser negativo.";
            if (fieldErrors.Count > 0)
                return BadRequest(new { title = "Validation Failed", fieldErrors });
            if (!_tenant.UserId.HasValue) return Unauthorized();
            var userId = _tenant.UserId.Value;

            // Resolver categoría automáticamente:
            // - Si viene CategoryId, validar que pertenece al usuario
            // - Si no viene, intentar por nombre 'Categoria'; si no existe, crear
            // - Si no hay nombre, usar o crear "General"
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var negocioId = _tenant.NegocioId.Value;
            if (producto.CategoryId != null)
            {
                var catOk = await _context.Categories
                    .Include(c => c.Usuario)
                    .AnyAsync(c => c.Id == producto.CategoryId && c.Usuario != null && c.Usuario.NegocioId == negocioId);
                if (!catOk)
                {
                    // Fallback tolerante: ignorar CategoryId inválido y resolver por nombre o General
                    producto.CategoryId = null;
                }
            }
            if (producto.CategoryId == null)
            {
                int categoryId;
                if (!string.IsNullOrWhiteSpace(producto.Categoria))
                {
                    var nombreCat = producto.Categoria.Trim();
                    var cat = await _context.Categories.Include(c => c.Usuario)
                        .FirstOrDefaultAsync(c => c.Usuario != null && c.Usuario.NegocioId == negocioId && c.Name == nombreCat);
                    if (cat == null)
                    {
                        cat = new Category { Name = nombreCat, UsuarioId = userId };
                        _context.Categories.Add(cat);
                        await _context.SaveChangesAsync();
                    }
                    categoryId = cat.Id;
                }
                else
                {
                    var cat = await _context.Categories.Include(c => c.Usuario)
                        .FirstOrDefaultAsync(c => c.Usuario != null && c.Usuario.NegocioId == negocioId && c.Name == "General");
                    if (cat == null)
                    {
                        cat = new Category { Name = "General", UsuarioId = userId };
                        _context.Categories.Add(cat);
                        await _context.SaveChangesAsync();
                    }
                    categoryId = cat.Id;
                }
                producto.CategoryId = categoryId;
            }

            producto.UsuarioId = userId;
            // SIEMPRE calcular stock inicial automáticamente: CantidadInicial - Merma
            // Ignorar lo que venga del frontend para evitar inconsistencias
            var stockCalculado = (producto.CantidadInicial >= 0 ? producto.CantidadInicial : 0)
                                - (producto.Merma >= 0 ? producto.Merma : 0);
            if (stockCalculado < 0) stockCalculado = 0;
            producto.StockActual = stockCalculado;
            // Forzar negocio desde tenant (ignorar cualquier valor enviado por el cliente)
            producto.NegocioId = negocioId;
            
            if (producto.FechaRegistro == default)
            {
                producto.FechaRegistro = DateTime.UtcNow;
            }
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto);
        }

        // PUT: api/producto/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Producto producto)
        {
            if (id != producto.Id) return BadRequest(new { title = "Validation Failed", fieldErrors = new Dictionary<string, string> { { "id", "El id de la URL no coincide con el del objeto." } } });
            var fieldErrors = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(producto.Nombre))
                fieldErrors["nombre"] = "El nombre del producto es obligatorio.";
            if (producto.PrecioVenta <= 0)
                fieldErrors["precioVenta"] = "El precio de venta debe ser mayor a 0.";
            if (producto.PrecioCompra < 0)
                fieldErrors["precioCompra"] = "El precio de compra no puede ser negativo.";
            // Validación de negocio: precio venta > precio compra
            if (producto.PrecioVenta > 0 && producto.PrecioCompra > 0 && producto.PrecioVenta <= producto.PrecioCompra)
                fieldErrors["precioVenta"] = "El precio de venta debe ser mayor al precio de compra para tener ganancia.";
            if (producto.CantidadInicial < 0)
                fieldErrors["cantidadInicial"] = "La cantidad inicial no puede ser negativa.";
            if (producto.Merma < 0)
                fieldErrors["merma"] = "La merma no puede ser negativa.";
            // Validación: merma no puede exceder cantidad inicial (validar contra nuevo valor enviado)
            if (producto.CantidadInicial >= 0 && producto.Merma > producto.CantidadInicial)
                fieldErrors["merma"] = $"La merma ({producto.Merma}) no puede ser mayor a la cantidad inicial ({producto.CantidadInicial}).";
            if (producto.StockActual < 0)
                fieldErrors["stockActual"] = "El stock actual no puede ser negativo.";
            if (producto.StockMinimo < 0)
                fieldErrors["stockMinimo"] = "El stock mínimo no puede ser negativo.";
            if (fieldErrors.Count > 0)
                return BadRequest(new { title = "Validation Failed", fieldErrors });

            var existe = await _context.Productos.AnyAsync(p => p.Id == id);
            if (!existe) return NotFound(new { message = "Producto no encontrado." });

            // Attach and update allowed fields (use tenant)
            if (!_tenant.UserId.HasValue) return Unauthorized();
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var userId = _tenant.UserId.Value;

            var existing = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == _tenant.NegocioId.Value);
            if (existing == null) return NotFound(new { message = "Producto no encontrado o no pertenece a tu negocio." });
            existing.Nombre = producto.Nombre;
            existing.Descripcion = producto.Descripcion;
            existing.PrecioCompra = producto.PrecioCompra;
            existing.PrecioVenta = producto.PrecioVenta;
            existing.Categoria = producto.Categoria;
            existing.Subcategoria = producto.Subcategoria;
            // Resolver/validar categoría en updates
            // Obtener negocioId desde el producto/tenant
            var negocioId = existing.NegocioId;
            if (producto.CategoryId != null)
            {
                var catOk = await _context.Categories.Include(c => c.Usuario)
                    .AnyAsync(c => c.Id == producto.CategoryId && c.Usuario != null && c.Usuario.NegocioId == negocioId);
                if (catOk)
                {
                    existing.CategoryId = producto.CategoryId;
                }
                else
                {
                    // Fallback tolerante: ignorar CategoryId inválido y resolver por nombre o General
                    producto.CategoryId = null;
                }
            }
            if (producto.CategoryId == null)
            {
                int categoryId;
                if (!string.IsNullOrWhiteSpace(producto.Categoria))
                {
                    var nombreCat = producto.Categoria.Trim();
                    var cat = await _context.Categories.Include(c => c.Usuario)
                        .FirstOrDefaultAsync(c => c.Usuario != null && c.Usuario.NegocioId == negocioId && c.Name == nombreCat);
                    if (cat == null)
                    {
                        cat = new Category { Name = nombreCat, UsuarioId = userId };
                        _context.Categories.Add(cat);
                        await _context.SaveChangesAsync();
                    }
                    categoryId = cat.Id;
                }
                else
                {
                    var cat = await _context.Categories.Include(c => c.Usuario)
                        .FirstOrDefaultAsync(c => c.Usuario != null && c.Usuario.NegocioId == negocioId && c.Name == "General");
                    if (cat == null)
                    {
                        cat = new Category { Name = "General", UsuarioId = userId };
                        _context.Categories.Add(cat);
                        await _context.SaveChangesAsync();
                    }
                    categoryId = cat.Id;
                }
                existing.CategoryId = categoryId;
            }
            existing.CantidadInicial = producto.CantidadInicial;
            // Aplicar delta de merma automáticamente si aumenta y el stock no fue editado manualmente
            var mermaAnterior = existing.Merma;
            var mermaNueva = producto.Merma;
            existing.Merma = mermaNueva;
            if (mermaNueva > mermaAnterior && (producto.StockActual == existing.StockActual || producto.StockActual < 0))
            {
                var delta = mermaNueva - mermaAnterior;
                // Validar que el delta no exceda el stock actual
                if (delta > existing.StockActual)
                    return BadRequest(new { message = "El incremento de merma excede el stock actual." });
                existing.StockActual = existing.StockActual - delta;
            }
            // Recalcular stock solo si CantidadInicial o Merma cambiaron
            // Si el usuario editó StockActual manualmente (por ajuste de inventario), respetarlo
            if (producto.StockActual >= 0 && producto.StockActual != existing.StockActual)
            {
                // Usuario editó stock manualmente
                existing.StockActual = producto.StockActual;
            }
            // Si no vino stock o es el mismo, mantener el actual (no recalcular, se actualiza con ventas)
            existing.StockMinimo = producto.StockMinimo;
            existing.UnidadMedida = producto.UnidadMedida;
            existing.CodigoBarras = producto.CodigoBarras;
            existing.ImagenUrl = producto.ImagenUrl;
            existing.Activo = producto.Activo;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/producto/{id}  -> soft-delete
        // Solo Dueño y Gerente pueden eliminar productos
        [HttpDelete("{id}")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!_tenant.UserId.HasValue) return Unauthorized();
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var negocioId = _tenant.NegocioId.Value;

            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == negocioId);
            if (producto == null) return NotFound(new { message = "Producto no encontrado o no pertenece a tu negocio." });
            if (!producto.Activo) return BadRequest(new { message = "Producto ya inactivo." });
            producto.Activo = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PATCH: api/producto/{id}/active  -> set active true/false
        [HttpPatch("{id}/active")]
        public async Task<IActionResult> SetActive(int id, [FromBody] dynamic body)
        {
            bool? active = null;
            try { active = (bool?)body.active; } catch { }
            if (active == null) return BadRequest(new { message = "Campo 'active' requerido en body." });
            if (!_tenant.UserId.HasValue) return Unauthorized();
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var negocioId = _tenant.NegocioId.Value;

            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == negocioId);
            if (producto == null) return NotFound(new { message = "Producto no encontrado o no pertenece a tu negocio." });
            producto.Activo = active.Value;
            await _context.SaveChangesAsync();
            return Ok(new { producto.Id, producto.Activo });
        }

        // PUT: api/producto/{id}/activo  -> toggle activo (alias para frontend nuevo)
        [HttpPut("{id}/activo")]
        public async Task<IActionResult> ToggleActivo(int id, [FromBody] DTOs.ToggleActivoDTO dto)
        {
            if (!_tenant.UserId.HasValue) return Unauthorized();
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var negocioId = _tenant.NegocioId.Value;

            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == negocioId);
            if (producto == null) return NotFound(new { message = "Producto no encontrado o no pertenece a tu negocio." });
            
            producto.Activo = dto.Activo;
            await _context.SaveChangesAsync();
            return Ok(new { producto.Id, producto.Nombre, producto.Activo });
        }

        // PUT: api/producto/{id}/descuento  -> aplicar descuento (solo dueño/gerente)
        [HttpPut("{id}/descuento")]
        [Authorize(Roles = "dueño,Dueño,Dueno,gerente,Gerente")]
        public async Task<IActionResult> AplicarDescuento(int id, [FromBody] DTOs.AplicarDescuentoDTO dto)
        {
            if (!_tenant.UserId.HasValue) return Unauthorized();
            if (!_tenant.NegocioId.HasValue) return Unauthorized();
            var negocioId = _tenant.NegocioId.Value;

            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == negocioId);
            if (producto == null) return NotFound(new { message = "Producto no encontrado o no pertenece a tu negocio." });

            // Validaciones de descuento
            if (dto.Porcentaje.HasValue && (dto.Porcentaje < 0 || dto.Porcentaje > 100))
                return BadRequest(new { message = "El porcentaje de descuento debe estar entre 0 y 100." });
            if (dto.FechaInicio.HasValue && dto.FechaFin.HasValue && dto.FechaInicio > dto.FechaFin)
                return BadRequest(new { message = "La fecha de inicio no puede ser posterior a la fecha de fin." });
            if (dto.HoraInicio.HasValue && dto.HoraFin.HasValue && dto.HoraInicio > dto.HoraFin)
                return BadRequest(new { message = "La hora de inicio no puede ser posterior a la hora de fin." });

            // Aplicar descuento (null para remover)
            producto.DescuentoPorcentaje = dto.Porcentaje;
            producto.DescuentoFechaInicio = dto.FechaInicio;
            producto.DescuentoFechaFin = dto.FechaFin;
            producto.DescuentoHoraInicio = dto.HoraInicio;
            producto.DescuentoHoraFin = dto.HoraFin;

            await _context.SaveChangesAsync();
            return Ok(new
            {
                producto.Id,
                producto.DescuentoPorcentaje,
                producto.DescuentoFechaInicio,
                producto.DescuentoFechaFin,
                producto.DescuentoHoraInicio,
                producto.DescuentoHoraFin,
                message = dto.Porcentaje.HasValue ? "Descuento aplicado correctamente." : "Descuento removido."
            });
        }

        // GET: api/producto/inventario-pdf?categoria={categoria}&stockBajo={true/false}
        [HttpGet("inventario-pdf")]
        public async Task<IActionResult> GetInventarioPdf([FromQuery] string? categoria = null, [FromQuery] bool stockBajo = false)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var negocioId = await _context.Usuarios.Where(u => u.Id == userId).Select(u => u.NegocioId).FirstOrDefaultAsync();

            if (!negocioId.HasValue) return Unauthorized();

            try
            {
                var pdfBytes = _pdfService.GenerateInventarioPdf(negocioId.Value, categoria, stockBajo);
                var filename = "inventario";
                if (!string.IsNullOrEmpty(categoria)) filename += $"-{categoria}";
                if (stockBajo) filename += "-stock-bajo";
                filename += ".pdf";
                return File(pdfBytes, "application/pdf", filename);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
