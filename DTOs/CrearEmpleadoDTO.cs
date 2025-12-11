namespace VentifyAPI.DTOs
{
    public class CrearEmpleadoDTO
    {
        public string Nombre { get; set; } = null!;
        public string Apellido1 { get; set; } = null!;
        public string? Apellido2 { get; set; }
        public string Telefono { get; set; } = null!;
        public decimal SueldoDiario { get; set; }
        
        // Nuevos campos para Settings
        public string? RFC { get; set; }
        public string? NumeroSeguroSocial { get; set; }
        public string? Puesto { get; set; }
        public DateTime? FechaIngreso { get; set; }
    }
}
