using System;

namespace VentifyAPI.DTOs
{
    public class AplicarDescuentoDTO
    {
        public decimal? Porcentaje { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public TimeSpan? HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }
    }
}
