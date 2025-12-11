namespace VentifyAPI.DTOs
{
    public class PermisosExtraDTO
    {
        /// <summary>
        /// Lista de módulos extra: "inventario", "pos", "caja", "reportes", "clientes"
        /// </summary>
        public List<string> Modulos { get; set; } = new();
        
        /// <summary>
        /// Nota opcional explicando por qué se asignan estos permisos
        /// </summary>
        public string? Nota { get; set; }
    }
}
