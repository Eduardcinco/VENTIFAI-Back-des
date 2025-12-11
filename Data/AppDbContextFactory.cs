using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;
using VentifyAPI.Models;

namespace VentifyAPI.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true);
            IConfigurationRoot configuration = builder.Build();

            var envConn = Environment.GetEnvironmentVariable("MYSQL_CONN");
            var connectionString = !string.IsNullOrEmpty(envConn) ? envConn : configuration.GetConnectionString("MySqlConnection");
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            // For design-time tools, provide a TenantContext with no NegocioId so filters are not restrictive
            var tenant = new VentifyAPI.Services.TenantContext { NegocioId = null, UserId = null };
            return new AppDbContext(optionsBuilder.Options, tenant);
        }
    }
}
