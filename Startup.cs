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
            // 1. TENANT CONTEXT
            // ============================================================
            services.AddScoped<VentifyAPI.Services.ITenantContext, VentifyAPI.Services.TenantContext>();

            // ============================================================
            // 2. DATABASE (RAILWAY SAFE)
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
            // 3. JWT
            // ============================================================
            var jwtSecret =
                Configuration["Jwt:Secret"]
                ?? Environment.GetEnvironmentVariable("Jwt__Secret");

            if (string.IsNullOrWhiteSpace(jwtSecret))
                throw new Exception("❌ JWT secret NOT configured.");

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
            // 4. COOKIE POLICY (Railway HTTPS + Cross-site)
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
            // 5. CORS (CRITICAL: AllowCredentials + WithOrigins, no AllowAnyOrigin)
            // ============================================================
            services.AddCors(options =>
            {
                options.AddPolicy("AllowNetlify", builder =>
                    builder.WithOrigins(
                        "https://ventifive.netlify.app"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                );
            });
            services.AddCookiePolicy(options =>
            {
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None;
            });

            // ============================================================
            // 6. SERVICES
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

            // Debug middleware para loguear cookies (SOLO en Development)
            if (env.IsDevelopment())
                app.UseMiddleware<VentifyAPI.Middleware.CookieDebugMiddleware>();

            app.UseRouting();
            app.UseCors("AllowNetlify"); // CORS debe ir antes de auth
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
