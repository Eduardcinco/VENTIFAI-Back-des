namespace VentifyAPI.DTOs
{
    public class CambiarPasswordDTO
    {
        public required string PasswordActual { get; set; }
        public required string PasswordNueva { get; set; }
    }
}
