namespace VentifyAPI.DTOs
{
    public class RegistroUsuarioDTO
    {
        public required string Nombre { get; set; }
        public required string Correo { get; set; }
        public required string Password { get; set; }
    }
}
