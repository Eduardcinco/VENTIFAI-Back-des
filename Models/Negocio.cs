namespace VentifyAPI.Models
{
    public class Negocio
    {
        public int Id { get; set; }

        public required string NombreNegocio { get; set; }
        public int OwnerId { get; set; } // Dueño del negocio (usuario)
        
        // Información de contacto y fiscal
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? RFC { get; set; }
        public string? GiroComercial { get; set; }
        
        // Branding (personalización visual)
        public string? ColorPrimario { get; set; }
        public string? ColorSecundario { get; set; }
        public string? ColorFondo { get; set; }
        public string? ColorAcento { get; set; }
        public bool ModoOscuro { get; set; } = false;

        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

        // Relaciones
        public ICollection<PuntoDeVenta> PuntosDeVenta { get; set; } = new List<PuntoDeVenta>();
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}

