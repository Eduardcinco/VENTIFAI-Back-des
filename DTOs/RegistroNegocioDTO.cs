namespace VentifyAPI.DTOs
{
    public class RegistroNegocioDTO
    {
        public required string NombreNegocio { get; set; }
        public required string PropietarioNombre { get; set; }
        public required string Correo { get; set; }
        public required string Password { get; set; }
    }
}
