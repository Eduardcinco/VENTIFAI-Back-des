using VentifyAPI.DTOs;

namespace VentifyAPI.Services
{
    public interface INegocioService
    {
        Task<int> RegistrarNegocioAsync(RegistroNegocioDTO dto);
    }
}
