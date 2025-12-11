using System;

namespace VentifyAPI.Models
{
    public class Caja
    {
        public int Id { get; set; }
        public int NegocioId { get; set; }
        public int UsuarioAperturaId { get; set; }
        public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
        public decimal MontoInicial { get; set; }
        public decimal MontoActual { get; set; }
        public bool Abierta { get; set; } = true;
        public DateTime? FechaCierre { get; set; }
        public int? UsuarioCierreId { get; set; }  // Quién cerró la caja
        public decimal? MontoCierre { get; set; }
        public string? ResumenCierre { get; set; }
        public string? AbiertaPor { get; set; } // Nombre del usuario que abrió (auditoría legible)
        public string? Turno { get; set; } // Turno declarado (Matutino/Vespertino/Nocturno u otro)
    }
}
