using WMSPro.Application.Dashboard.Handlers;
using WMSPro.Infrastructure;
using WMSPro.Infrastructure.Persistence;
using WMSPro.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Infrastructure (DB, Identity, JWT, Repositories) ─────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── MediatR (scan Application assembly) ───────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetDashboardStatsHandler).Assembly));

// ── CORS (for MVC client) ─────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("MvcClient", policy =>
        policy.WithOrigins("https://localhost:7001", "http://localhost:5001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("MvcClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ── Seed Database ─────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await DatabaseSeeder.SeedRolesAndAdminAsync(roleManager, userManager);
    await DatabaseSeeder.SeedInitialDataAsync(dbContext);
}

app.Run();
