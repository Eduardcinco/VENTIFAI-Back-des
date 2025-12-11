using System.ComponentModel.DataAnnotations;

namespace VentifyAPI.DTOs
{
    public class MovimientoCajaDTO
    {
        [Required(ErrorMessage = "El tipo es requerido")]
        [RegularExpression("^(entrada|salida)$", ErrorMessage = "El tipo debe ser 'entrada' o 'salida'")]
        public string Tipo { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El monto es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal Monto { get; set; }
        
        [Required(ErrorMessage = "La categoría es requerida")]
        [MaxLength(100, ErrorMessage = "La categoría no puede exceder 100 caracteres")]
        public string Categoria { get; set; } = string.Empty;
        
        [MaxLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Descripcion { get; set; }
        
        [MaxLength(50, ErrorMessage = "El método de pago no puede exceder 50 caracteres")]
        public string? MetodoPago { get; set; }
        
        [MaxLength(100, ErrorMessage = "La referencia no puede exceder 100 caracteres")]
        public string? Referencia { get; set; }
    }
}
