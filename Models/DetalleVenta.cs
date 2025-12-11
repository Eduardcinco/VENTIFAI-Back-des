namespace VentifyAPI.Models
{
    public class DetalleVenta
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public int ProductoId { get; set; }
        public int? VarianteProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }

        // Relaciones
        public Venta? Venta { get; set; }
        public Producto? Producto { get; set; }
        public VarianteProducto? VarianteProducto { get; set; }
    }
}
