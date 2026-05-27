using Microsoft.EntityFrameworkCore;
using BinayatiBackend.Models;

namespace BinayatiBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RentIncreaseHistory> RentIncreaseHistories => Set<RentIncreaseHistory>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(t => t.NationalId).IsUnique();
        });

        modelBuilder.Entity<Building>(entity =>
        {
            entity.HasMany(b => b.Floors)
                  .WithOne(f => f.Building)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Floor>(entity =>
        {
            entity.HasMany(f => f.Units)
                  .WithOne(u => u.Floor)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasMany(u => u.Contracts)
                  .WithOne(c => c.Unit)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.MaintenanceRequests)
                  .WithOne(m => m.Unit)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasMany(c => c.Payments)
                  .WithOne(p => p.Contract)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.RentIncreaseHistories)
                  .WithOne(r => r.Contract)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
