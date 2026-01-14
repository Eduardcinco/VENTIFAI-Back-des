using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VentifyAPI.Models;

namespace VentifyAPI.Services
{
    public interface ITokenService
    {
        (string accessToken, string refreshToken, DateTime refreshExpiresAt) CreateTokens(Usuario user);
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public (string accessToken, string refreshToken, DateTime refreshExpiresAt) CreateTokens(Usuario user)
        {
            // Intentar obtener del environment primero, luego del config
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET") 
                ?? _config["JWT_SECRET"]
                ?? _config["Jwt:Key"]
                ?? "fallback_secret_please_configure";

            var expiresMinutes = int.TryParse(
                Environment.GetEnvironmentVariable("JWT_EXPIRES_MINUTES") 
                ?? _config["JWT_EXPIRES_MINUTES"]
                ?? _config["Jwt:ExpiresInMinutes"], 
                out var m) ? m : 15;

            var refreshDays = int.TryParse(
                Environment.GetEnvironmentVariable("REFRESH_EXPIRES_DAYS") 
                ?? _config["REFRESH_EXPIRES_DAYS"]
                ?? _config["Jwt:RefreshExpirationDays"], 
                out var d) ? d : 30;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Leer rol actualizado de la base de datos (por si cambió)
            var rol = user.Rol ?? string.Empty;
            var claimList = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Nombre ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Correo ?? string.Empty),
                new Claim("nombre", user.Nombre ?? string.Empty),
                // Incluir rol en el claim estándar para que [Authorize(Roles=...)] funcione
                new Claim(ClaimTypes.Role, rol),
                // Mantener claim "rol" para el frontend si lo usa
                new Claim("rol", rol),
                // Control de sesiones: versión del token
                new Claim("tokenVersion", (user.TokenVersion).ToString())
            };

            if (user.NegocioId.HasValue)
            {
                claimList.Add(new Claim("negocioId", user.NegocioId.Value.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JWT_ISSUER") 
                    ?? _config["JWT_ISSUER"]
                    ?? _config["Jwt:Issuer"],
                audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                    ?? _config["JWT_AUDIENCE"]
                    ?? _config["Jwt:Audience"],
                claims: claimList,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + "." + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var refreshExpiresAt = DateTime.UtcNow.AddDays(refreshDays);

            return (accessToken, refreshToken, refreshExpiresAt);
        }
    }
}
