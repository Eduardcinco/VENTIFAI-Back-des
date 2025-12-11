namespace VentifyAPI.Models
{
    public class PuntoDeVenta
    {
        public int Id { get; set; }

        public required string NombrePunto { get; set; }

        // FK hacia Negocio
        public int NegocioId { get; set; }

        // Navegacion (nullable para evitar requerirla en inicializadores)
        public Negocio? Negocio { get; set; }
    }
}
