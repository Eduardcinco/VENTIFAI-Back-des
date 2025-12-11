using System.Collections.Generic;

namespace VentifyAPI.DTOs
{
    public class VentaItemDTO
    {
        public int ProductoId { get; set; }
        public int? VarianteProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
    }

    public class VentaCreateDTO
    {
        public List<VentaItemDTO> Items { get; set; } = new List<VentaItemDTO>();
        public decimal Total { get; set; }
        public string? PaymentMethod { get; set; }
        public int? ClienteId { get; set; }
        public int? CajaId { get; set; }
        public decimal? MontoRecibido { get; set; }
        public decimal? Cambio { get; set; }
    }
}
