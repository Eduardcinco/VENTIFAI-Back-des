using System;

namespace VentifyAPI.Models
{
    public class VarianteProducto
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string? Nombre { get; set; } // Ej: "600 ml", "Rojo", "Grande"
        public decimal? Precio { get; set; }
        public int Stock { get; set; }
        public string? Codigo { get; set; }

        // Relación
        public Producto? Producto { get; set; }
    }
}
