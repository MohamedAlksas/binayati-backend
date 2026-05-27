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

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

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

        // Seed floors & units (runs if structure doesn't match)
        if (!await db.Units.AnyAsync(u => u.UnitNumber == "G1" && u.Type == "Shop"))
        {
            // Remove old data first
            db.Payments.RemoveRange(db.Payments);
            db.RentIncreaseHistories.RemoveRange(db.RentIncreaseHistories);
            db.MaintenanceRequests.RemoveRange(db.MaintenanceRequests);
            db.Notifications.RemoveRange(db.Notifications);
            db.Contracts.RemoveRange(db.Contracts);
            db.Units.RemoveRange(db.Units);
            db.Floors.RemoveRange(db.Floors);
            db.Tenants.RemoveRange(db.Tenants);
            await db.SaveChangesAsync();

            var building = await db.Buildings.FirstAsync();
            var ground = new BinayatiBackend.Models.Floor { BuildingId = building.Id, FloorNumber = 0, Label = "Ground" };
            var second = new BinayatiBackend.Models.Floor { BuildingId = building.Id, FloorNumber = 2, Label = "2nd" };
            var third = new BinayatiBackend.Models.Floor { BuildingId = building.Id, FloorNumber = 3, Label = "3rd" };
            var fourth = new BinayatiBackend.Models.Floor { BuildingId = building.Id, FloorNumber = 4, Label = "4th" };
            var fifth = new BinayatiBackend.Models.Floor { BuildingId = building.Id, FloorNumber = 5, Label = "5th" };
            db.Floors.AddRange(ground, second, third, fourth, fifth);
            await db.SaveChangesAsync();

            db.Units.AddRange(
                new BinayatiBackend.Models.Unit { FloorId = ground.Id, UnitNumber = "G1", Type = "Shop", Description = "Ground floor shop" },
                new BinayatiBackend.Models.Unit { FloorId = second.Id, UnitNumber = "201", Type = "Apartment", Description = "2BR, 120m²" },
                new BinayatiBackend.Models.Unit { FloorId = second.Id, UnitNumber = "202", Type = "Apartment", Description = "1BR, 80m²" },
                new BinayatiBackend.Models.Unit { FloorId = third.Id, UnitNumber = "301", Type = "Apartment", Description = "3BR, 150m²" },
                new BinayatiBackend.Models.Unit { FloorId = third.Id, UnitNumber = "302", Type = "Apartment", Description = "2BR, 100m²" },
                new BinayatiBackend.Models.Unit { FloorId = fourth.Id, UnitNumber = "401", Type = "Apartment", Description = "2BR, 110m²" },
                new BinayatiBackend.Models.Unit { FloorId = fifth.Id, UnitNumber = "501", Type = "Apartment", Description = "3BR, 160m²" }
            );
            await db.SaveChangesAsync();
        }

        // Seed tenants & contracts
        if (!await db.Tenants.AnyAsync(t => t.NationalId == "29801011234567"))
        {
            db.Tenants.AddRange(
                new BinayatiBackend.Models.Tenant { Name = "أحمد علي", PhoneNumber = "01234567890", Email = "ahmed@example.com", NationalId = "29801011234567" },
                new BinayatiBackend.Models.Tenant { Name = "محمد حسن", PhoneNumber = "01123456789", Email = "mohamed@example.com", NationalId = "28503041234567" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Contracts.AnyAsync())
        {
            var shop = await db.Units.FirstAsync(u => u.UnitNumber == "G1");
            var apt = await db.Units.FirstAsync(u => u.UnitNumber == "201");
            var ahmed = await db.Tenants.FirstAsync(t => t.NationalId == "29801011234567");
            var mohamed = await db.Tenants.FirstAsync(t => t.NationalId == "28503041234567");

            var contract1 = new BinayatiBackend.Models.Contract
            {
                UnitId = shop.Id, TenantId = ahmed.Id,
                StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                RentAmount = 5000, AnnualIncreasePercent = 10, SecurityDeposit = 10000,
                Status = "Active", Notes = "Annual increase every January"
            };
            var contract2 = new BinayatiBackend.Models.Contract
            {
                UnitId = apt.Id, TenantId = mohamed.Id,
                StartDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc),
                RentAmount = 3000, AnnualIncreasePercent = 8, SecurityDeposit = 6000,
                Status = "Active", Notes = ""
            };
            db.Contracts.AddRange(contract1, contract2);
            await db.SaveChangesAsync();

            // Mark units as occupied
            shop.IsOccupied = true;
            apt.IsOccupied = true;
            await db.SaveChangesAsync();

            // Seed payments
            db.Payments.AddRange(
                new BinayatiBackend.Models.Payment { ContractId = contract1.Id, Amount = 5000, PaidDate = new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc), PeriodStart = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), PeriodEnd = new DateTime(2025, 1, 31, 0, 0, 0, DateTimeKind.Utc), Method = "Cash" },
                new BinayatiBackend.Models.Payment { ContractId = contract1.Id, Amount = 5000, PaidDate = new DateTime(2025, 2, 3, 0, 0, 0, DateTimeKind.Utc), PeriodStart = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc), PeriodEnd = new DateTime(2025, 2, 28, 0, 0, 0, DateTimeKind.Utc), Method = "BankTransfer" },
                new BinayatiBackend.Models.Payment { ContractId = contract2.Id, Amount = 3000, PaidDate = new DateTime(2025, 3, 5, 0, 0, 0, DateTimeKind.Utc), PeriodStart = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc), PeriodEnd = new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Utc), Method = "Cash" }
            );
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
