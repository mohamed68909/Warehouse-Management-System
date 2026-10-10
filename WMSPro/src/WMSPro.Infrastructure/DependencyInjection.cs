using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WMSPro.Application.Abstractions;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Application.Auth;
using WMSPro.Infrastructure.Auth;
using WMSPro.Infrastructure.Persistence;
using WMSPro.Infrastructure.Repositories;
using WMSPro.Infrastructure.Services;

namespace WMSPro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Setup Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Setup Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Setup JWT
        var jwtSettings = new JwtSettings();
        configuration.Bind(JwtSettings.SectionName, jwtSettings);
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Secret))
            };
        });

        // Register Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IPdfReportService, PdfReportService>();
        services.AddScoped<WMSPro.Application.Picking.Services.IPickPathOptimizer, WMSPro.Application.Picking.Services.PickPathOptimizer>();

        // Register Repositories
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IWarehouseStockRepository, WarehouseStockRepository>();
        services.AddScoped<IInventoryTransactionRepository, InventoryTransactionRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IStorageLocationRepository, StorageLocationRepository>();
        services.AddScoped<IPickingWaveRepository, PickingWaveRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
