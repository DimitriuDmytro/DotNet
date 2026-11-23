var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

app.Run();
