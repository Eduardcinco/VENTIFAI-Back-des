using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;

namespace VentifyAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // ============================================================
            // 1. REGISTRAR TENANT CONTEXT (RESUELVE EL ERROR PRINCIPAL)
            // ============================================================
            services.AddScoped<VentifyAPI.Services.ITenantContext, VentifyAPI.Services.TenantContext>();

            // ============================================================
            // 2. CONFIGURAR BASE DE DATOS
            // ============================================================
            var connectionString = Configuration.GetConnectionString("MySqlConnection") 
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__MySqlConnection")
                ?? "Server=localhost;Database=ventify;User=root;Password=password;";

            services.AddDbContext<VentifyAPI.Data.AppDbContext>(options =>
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString)
                )
            );

            // ============================================================
            // 3. CONFIGURAR JWT AUTHENTICATION
            // ============================================================
            var jwtSecret = Configuration["Jwt:Secret"] 
                ?? Environment.GetEnvironmentVariable("Jwt__Secret")
                ?? "ventify-super-secret-key-change-this-in-production-minimum-32-characters-long";
            
            var key = Encoding.ASCII.GetBytes(jwtSecret);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                // Permitir leer token desde cookies
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.ContainsKey("access_token"))
                        {
                            context.Token = context.Request.Cookies["access_token"];
                        }
                        return System.Threading.Tasks.Task.CompletedTask;
                    }
                };
            });

            // ============================================================
            // 4. CONFIGURAR CORS
            // ============================================================
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                );
            });

            // ============================================================
            // 5. REGISTRAR SERVICIOS NECESARIOS
            // ============================================================
            services.AddScoped<VentifyAPI.Services.ITokenService, VentifyAPI.Services.TokenService>();
            services.AddScoped<VentifyAPI.Services.PdfService>();
            services.AddScoped<VentifyAPI.Services.TicketService>();
            services.AddScoped<VentifyAPI.Services.AiService>();
            services.AddHttpClient(); // Para AiService

            // ============================================================
            // 6. AGREGAR CONTROLADORES
            // ============================================================
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors("AllowFrontend");
            
            // IMPORTANTE: Authentication ANTES de Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}