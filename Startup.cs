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
            // 1. TENANT CONTEXT
            // ============================================================
            services.AddScoped<VentifyAPI.Services.ITenantContext, VentifyAPI.Services.TenantContext>();

            // ============================================================
            // 2. DATABASE (RAILWAY SAFE)
            // ============================================================
            var connectionString =
                Configuration.GetConnectionString("MySqlConnection")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__MySqlConnection");

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
            // 4. CORS
            // ============================================================
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", builder =>
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod());
            });

            // ============================================================
            // 5. SERVICES
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
            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
