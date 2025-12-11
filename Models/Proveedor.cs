using System.ComponentModel.DataAnnotations;
namespace VentifyAPI.Models
{
    public class Proveedor
    {
        public int Id { get; set; }
        [Required]
        public string Nombre { get; set; } = null!;
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public int? NegocioId { get; set; }
    }
}
