namespace VentifyAPI.DTOs
{
    public class DevResetPasswordDTO
    {
        public required string Correo { get; set; }
        public required string NuevaPassword { get; set; }
    }
}
