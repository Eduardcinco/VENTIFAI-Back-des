using System;
using System.Collections.Generic;

namespace VentifyAPI.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        // legacy string fields kept for compatibility; prefer using CategoryId
        public string? Categoria { get; set; }
        public string? Subcategoria { get; set; }

        // New: link to persistent Category entity
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        
        // Ownership: producto pertenece a un usuario
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        // Negocio propietario (se establece desde el tenant en el backend)
        public int NegocioId { get; set; }
        public int CantidadInicial { get; set; }
        public int Merma { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public string? UnidadMedida { get; set; }
        public string? CodigoBarras { get; set; }
        public string? ImagenUrl { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true;

        // Descuentos (solo dueño puede establecer)
        public decimal? DescuentoPorcentaje { get; set; }
        public DateTime? DescuentoFechaInicio { get; set; }
        public DateTime? DescuentoFechaFin { get; set; }
        public TimeSpan? DescuentoHoraInicio { get; set; }
        public TimeSpan? DescuentoHoraFin { get; set; }

        // Relaciones
        public ICollection<VarianteProducto> Variantes { get; set; } = new List<VarianteProducto>();
    }
}
