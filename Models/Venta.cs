using System;
using System.Collections.Generic;

namespace VentifyAPI.Models
{
    public class Venta
    {
        public int Id { get; set; }
        public int NegocioId { get; set; }
        public int UsuarioId { get; set; }
        // Cliente eliminado
        public decimal TotalPagado { get; set; }
        public string FormaPago { get; set; } = "Efectivo";
        public decimal? MontoRecibido { get; set; }
        public decimal? Cambio { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;
        public string? Ticket { get; set; }

        // Relaciones
        public Negocio? Negocio { get; set; }
        public Usuario? Usuario { get; set; }
        // Cliente eliminado
        public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    }
}
