namespace VentifyAPI.DTOs
{
    /// <summary>
    /// DTO para filtrar reportes de ventas
    /// </summary>
    public class FiltroReporteDTO
    {
        /// <summary>
        /// Fecha de inicio del reporte (requerido)
        /// </summary>
        public DateTime FechaInicio { get; set; }

        /// <summary>
        /// Fecha de fin del reporte (requerido)
        /// </summary>
        public DateTime FechaFin { get; set; }

        /// <summary>
        /// Tipo de agrupación: dia, semana, mes, anio
        /// </summary>
        public string TipoAgrupacion { get; set; } = "dia";

        /// <summary>
        /// Formato de exportación: excel, pdf
        /// </summary>
        public string? Formato { get; set; }

        /// <summary>
        /// IDs de categorías específicas (opcional)
        /// </summary>
        public List<int>? CategoriasIds { get; set; }

        /// <summary>
        /// IDs de cajeros específicos (opcional)
        /// </summary>
        public List<int>? CajerosIds { get; set; }

        /// <summary>
        /// Método de pago específico (opcional): efectivo, tarjeta, transferencia
        /// </summary>
        public string? MetodoPago { get; set; }
    }
}
