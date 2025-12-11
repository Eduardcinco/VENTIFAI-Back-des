namespace VentifyAPI.DTOs
{
    public class NegocioPerfilDTO
    {
        public int Id { get; set; }
        public string NombreNegocio { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? RFC { get; set; }
        public string? GiroComercial { get; set; }
    }
}
