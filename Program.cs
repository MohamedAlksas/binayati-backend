using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BinayatiBackend.Data;
using BinayatiBackend.Services;

Console.WriteLine("Starting Binayati backend..."); 

try
{
var builder = WebApplication.CreateBuilder(args);

var connStr = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connStr);
});

var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var hasBuilding = await db.Buildings.AnyAsync();
    if (!hasBuilding)
    {
        db.Buildings.Add(new BinayatiBackend.Models.Building
        {
            Name = "المبنى",
            Address = "",
        });
        await db.SaveChangesAsync();
    }

    var hasOwner = await db.Users.AnyAsync(u => u.Role == "Owner");
    if (!hasOwner)
    {
        db.Users.Add(new BinayatiBackend.Models.User
        {
            FullName = "Owner",
            Email = "owner@binayati.com",
            PasswordHash = PasswordService.Hash("admin123"),
            Role = "Owner",
        });
        await db.SaveChangesAsync();
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { status = "running", app = "Binayati" }));

Console.WriteLine("Binayati backend started successfully");
app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    throw;
}
