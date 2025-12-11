using Microsoft.EntityFrameworkCore;
using VentifyAPI.Data;
using VentifyAPI.DTOs;
using VentifyAPI.Models;

namespace VentifyAPI.Services
{
    public class NegocioService : INegocioService
    {
        private readonly AppDbContext _context;

        public NegocioService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> RegistrarNegocioAsync(RegistroNegocioDTO dto)
        {
            // Crear negocio
            var negocio = new Negocio
            {
                NombreNegocio = dto.NombreNegocio
            };

            _context.Negocios.Add(negocio);
            await _context.SaveChangesAsync();

            // Crear usuario propietario
            var usuario = new Usuario
            {
                Nombre = dto.PropietarioNombre,
                Correo = dto.Correo,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                NegocioId = negocio.Id,
                Rol = "dueño"
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Crear punto de venta principal (si tabla existe)
            var punto = new PuntoDeVenta
            {
                NombrePunto = "Caja Principal",
                NegocioId = negocio.Id
            };

            _context.PuntosDeVenta.Add(punto);
            await _context.SaveChangesAsync();

            return negocio.Id;
        }
    }
}
