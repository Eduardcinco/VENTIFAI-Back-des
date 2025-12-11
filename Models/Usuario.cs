namespace VentifyAPI.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public required string Nombre { get; set; }
        public required string Correo { get; set; }
        public required string Password { get; set; }

        // FK hacia Negocio (opcional - se asigna cuando registra su negocio)
        public int? NegocioId { get; set; }
        public Negocio? Negocio { get; set; }

        // Rol del usuario en el negocio
        public string Rol { get; set; } = "dueño";

        // Campos para empleados
        public string? Apellido1 { get; set; }
        public string? Apellido2 { get; set; }
        public string? Telefono { get; set; }
        public decimal? SueldoDiario { get; set; }
        
        // Información fiscal y laboral
        public string? RFC { get; set; }
        public string? NumeroSeguroSocial { get; set; }
        public string? Puesto { get; set; }
        public DateTime? FechaIngreso { get; set; }
        
        // Foto de perfil
        public string? FotoPerfil { get; set; }
        
        // Token version para invalidar sesiones
        public int TokenVersion { get; set; } = 0;
        
        // Primer acceso - obliga a cambiar contraseña
        public bool PrimerAcceso { get; set; } = false;

        // Permisos extra (módulos adicionales temporales)
        // JSON string: ["inventario", "pos", "caja", "reportes", "clientes"]
        public string? PermisosExtra { get; set; }
        public int? PermisosExtraAsignadoPor { get; set; }
        public DateTime? PermisosExtraFecha { get; set; }
        public string? PermisosExtraNota { get; set; }

        // Timestamp
        public DateTime CreadoEn { get; set; } = DateTime.Now;
    }
}
