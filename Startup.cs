using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
            // 1. HTTP CONTEXT ACCESSOR (CRÍTICO para TenantContext)
            // ============================================================
            services.AddHttpContextAccessor();

            // ============================================================
            // 2. TENANT CONTEXT
            // ============================================================
            services.AddScoped<VentifyAPI.Services.ITenantContext, VentifyAPI.Services.TenantContext>();

            // ============================================================
            // 3. DATABASE (RAILWAY SAFE)
            // ============================================================
            var connectionString =
                Configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("❌ MySQL connection string NOT configured.");

            services.AddDbContext<VentifyAPI.Data.AppDbContext>(options =>
                options.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 21)),
                    mysql => mysql.EnableRetryOnFailure(
                        5,
                        TimeSpan.FromSeconds(10),
                        null
                    )
                )
            );

            // ============================================================
            // 4. JWT
            // ============================================================
            var jwtSecret =
                Configuration["Jwt:Key"]
                ?? Configuration["Jwt:Secret"]
                ?? Environment.GetEnvironmentVariable("Jwt__Key")
                ?? Environment.GetEnvironmentVariable("Jwt__Secret");

            if (string.IsNullOrWhiteSpace(jwtSecret))
                throw new Exception("❌ JWT secret/key NOT configured in appsettings or environment variables.");

            var key = Encoding.ASCII.GetBytes(jwtSecret);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.Request.Cookies.ContainsKey("access_token"))
                                context.Token = context.Request.Cookies["access_token"];
                            return System.Threading.Tasks.Task.CompletedTask;
                        }
                    };
                });

            // ============================================================
            // 5. COOKIE POLICY (Railway HTTPS + Cross-site)
            // ============================================================
            services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
            {
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // ============================================================
            // 6. CORS (CRITICAL: AllowCredentials + WithOrigins, no AllowAnyOrigin)
            // FORCE RAILWAY REDEPLOY: 2025-01-13
            // ============================================================
            services.AddCors(options =>
            {
                options.AddPolicy("AllowVentifive", builder =>
                {
                    builder
                        .WithOrigins(
                            "https://ventifive.netlify.app",
                            "http://localhost:4200"
                        )
                        // Métodos explícitos
                        .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH", "HEAD")
                        // Headers explícitos
                        .WithHeaders("Content-Type", "Authorization", "Accept", "X-Requested-With", "X-Debug-Negocio")
                        // CRÍTICO: Permite cookies y credenciales
                        .AllowCredentials()
                        // Caché preflight
                        .SetPreflightMaxAge(TimeSpan.FromSeconds(3600));
                });
            });
            services.AddCookiePolicy(options =>
            {
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None;
            });

            // ============================================================
            // 7. SERVICES
            // ============================================================
            services.AddScoped<VentifyAPI.Services.ITokenService, VentifyAPI.Services.TokenService>();
            services.AddScoped<VentifyAPI.Services.PdfService>();
            services.AddScoped<VentifyAPI.Services.TicketService>();
            services.AddScoped<VentifyAPI.Services.AiService>();
            services.AddHttpClient();

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();

            // ============================================================
            // MIDDLEWARE PIPELINE ORDER (CRÍTICO)
            // ============================================================
            // 1. Custom logging middleware para debug CORS (solo desarrollo)
            if (env.IsDevelopment())
            {
                app.UseMiddleware<VentifyAPI.Middleware.CorsDebugMiddleware>();
            }

            // 2. TenantMiddleware - Extrae y popula NegocioId desde JWT
            app.UseMiddleware<VentifyAPI.Middleware.TenantMiddleware>();

            // 3. CORS - DEBE ir ANTES de Authentication
            app.UseCors("AllowVentifive");

            app.UseCookiePolicy();

            // 4. Authentication y Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
