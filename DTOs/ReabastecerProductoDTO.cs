using System.ComponentModel.DataAnnotations;

namespace VentifyAPI.DTOs
{
    public class ReabastecerProductoDTO
    {
        public decimal? PrecioCompra { get; set; }
        public decimal? PrecioVenta { get; set; }
        
        [Required(ErrorMessage = "La cantidad comprada es requerida")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int CantidadComprada { get; set; }
        
        [Range(0, int.MaxValue, ErrorMessage = "La merma no puede ser negativa")]
        public int? Merma { get; set; }
        
        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo")]
        public int? StockMinimo { get; set; }
    }
}
