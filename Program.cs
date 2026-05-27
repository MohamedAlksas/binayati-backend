using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
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

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { status = "running", app = "Binayati" }));

// Background DB init
_ = Task.Run(async () =>
{
    try
    {
        Console.WriteLine("DB init starting...");
        var connStr = builder.Configuration.GetConnectionString("Default");
        Console.WriteLine($"Connecting to DB...");
        using var rawConn = new NpgsqlConnection(connStr);
        await rawConn.OpenAsync();
        Console.WriteLine("Raw connection OK");
        using var cmd = rawConn.CreateCommand();
        cmd.CommandText = "SELECT version()";
        var version = await cmd.ExecuteScalarAsync();
        Console.WriteLine($"DB version: {version}");
        await rawConn.CloseAsync();

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Console.WriteLine("DB init: EnsureCreated...");
        db.Database.EnsureCreated();
        Console.WriteLine("DB init: Seeding...");
        if (!await db.Buildings.AnyAsync())
        {
            db.Buildings.Add(new BinayatiBackend.Models.Building { Name = "المبنى", Address = "" });
            await db.SaveChangesAsync();
        }
        if (!await db.Users.AnyAsync(u => u.Role == "Owner"))
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
        Console.WriteLine("DB init completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DB init error: {ex.GetType().Name}: {ex.Message}");
    }
});

Console.WriteLine("Binayati backend started successfully");
app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    throw;
}
