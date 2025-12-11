namespace VentifyAPI.DTOs
{
    public class CerrarSesionesDTO
    {
        /// <summary>
        /// Si es true, mantiene la sesión actual activa y cierra las demás
        /// </summary>
        public bool MantenerSesionActual { get; set; } = false;

        /// <summary>
        /// El refresh token actual (opcional, para identificar la sesión a mantener)
        /// </summary>
        public string? RefreshToken { get; set; }
    }
}
