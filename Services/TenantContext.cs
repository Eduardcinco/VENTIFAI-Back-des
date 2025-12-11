using VentifyAPI.Data;

namespace VentifyAPI.Services
{
    public interface ITenantContext
    {
        int? NegocioId { get; set; }
        int? UserId { get; set; }
        string? Rol { get; set; }
    }

    public class TenantContext : ITenantContext
    {
        public int? NegocioId { get; set; }
        public int? UserId { get; set; }
        public string? Rol { get; set; }
    }
}
