using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Services.Auth.Api.Data;
using Services.Auth.Api.Models;
using Services.Auth.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("IdentityDb")
    ?? builder.Configuration.GetConnectionString("sql")
    ?? "Server=localhost,1433;Database=IdentityDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password policies (К1)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout policies (К1)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Email confirmation requirement (К1, К4)
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEmailSender, MockEmailSender>();

builder.Services.AddPlatformJwtBearer(builder.Configuration);
builder.Services.AddPermissionAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Auto-migration and initial seed
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = ["Admin", "User"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Seeded role {Role}", role);
            }
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = "admin@platform.local";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Platform",
                LastName = "Administrator"
            };
            var result = await userManager.CreateAsync(adminUser, "Admin123!Secure");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                await userManager.AddToRoleAsync(adminUser, "User");
                logger.LogInformation("Seeded default admin user {AdminEmail}", adminEmail);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not apply database migrations or seed data at startup (DB may not be ready).");
    }
}

app.Run();
