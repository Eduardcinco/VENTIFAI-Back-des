using System;

namespace VentifyAPI.Models
{
    public class MermaEvento
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }
        public int Cantidad { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public int NegocioId { get; set; }
        public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
        public int StockAntes { get; set; }
        public int StockDespues { get; set; }
        public int MermaAntes { get; set; }
        public int MermaDespues { get; set; }
    }
}
