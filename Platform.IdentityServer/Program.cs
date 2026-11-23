using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.IdentityServer.Data;
using Platform.IdentityServer.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("IdentityServerDb")
    ?? builder.Configuration.GetConnectionString("sql")
    ?? "Server=localhost,1433;Database=IdentityServerDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True";

var migrationsAssembly = typeof(Program).Assembly.GetName().Name;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
    options.EmitStaticAudienceClaim = true;
})
.AddConfigurationStore(options =>
{
    options.ConfigureDbContext = b =>
        b.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
})
.AddOperationalStore(options =>
{
    options.ConfigureDbContext = b =>
        b.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
    options.EnableTokenCleanup = true;
})
.AddAspNetIdentity<ApplicationUser>()
.AddDeveloperSigningCredential();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

app.UseIdentityServer();
app.UseAuthorization();

app.MapDefaultControllerRoute();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    await DatabaseInitializer.InitializeDatabaseAsync(scope.ServiceProvider, app.Logger);
}

app.Run();
