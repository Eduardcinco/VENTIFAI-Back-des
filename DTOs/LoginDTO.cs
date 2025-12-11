namespace VentifyAPI.DTOs
{
    public class LoginDTO
    {
        public string? Correo { get; set; }
        public string? Email { get; set; } // Tolerancia para front que envía "email"
        public required string Password { get; set; }

        // Propiedad computada para obtener el correo/email
        public string CorreoNormalizado => Correo ?? Email ?? "";
    }
}
