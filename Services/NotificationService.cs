using BinayatiBackend.Data;
using BinayatiBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace BinayatiBackend.Services;

public class NotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db) => _db = db;

    public async Task CreateAsync(int userId, string type, string title, string message, int? relatedId = null, string relatedType = "")
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityId = relatedId,
            RelatedEntityType = relatedType,
        });
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task CheckContractNotificationsAsync()
    {
        var owners = await _db.Users.Where(u => u.Role == "Owner").ToListAsync();
        var now = DateTime.UtcNow;
        var soon = now.AddDays(30);

        var expiringContracts = await _db.Contracts
            .Include(c => c.Unit)
            .Include(c => c.Tenant)
            .Where(c => c.Status == "Active" && c.EndDate <= soon && c.EndDate >= now)
            .ToListAsync();

        foreach (var owner in owners)
        {
            foreach (var contract in expiringContracts)
            {
                var daysLeft = (contract.EndDate - now).Days;
                var exists = await _db.Notifications.AnyAsync(n =>
                    n.UserId == owner.Id &&
                    n.Type == "ContractExpiring" &&
                    n.RelatedEntityId == contract.Id &&
                    n.CreatedAt > now.AddDays(-1));

                if (!exists)
                {
                    await CreateAsync(
                        owner.Id,
                        "ContractExpiring",
                        $"عقد على وشك الانتهاء",
                        $"عقد {contract.Tenant?.Name} للوحدة {contract.Unit?.UnitNumber} ينتهي بعد {daysLeft} يوم",
                        contract.Id, "Contract"
                    );
                }
            }
        }

        var increaseDueContracts = await _db.Contracts
            .Include(c => c.Unit)
            .Include(c => c.Tenant)
            .Where(c => c.Status == "Active")
            .ToListAsync();

        foreach (var contract in increaseDueContracts)
        {
            var lastIncrease = await _db.RentIncreaseHistories
                .Where(r => r.ContractId == contract.Id)
                .OrderByDescending(r => r.AppliedDate)
                .FirstOrDefaultAsync();

            var lastIncreaseDate = lastIncrease?.AppliedDate ?? contract.StartDate;
            var nextIncreaseDate = lastIncreaseDate.AddYears(1);
            var monthsUntilIncrease = (nextIncreaseDate.Month - now.Month) + (nextIncreaseDate.Year - now.Year) * 12;

            if (monthsUntilIncrease >= -1 && monthsUntilIncrease <= 1)
            {
                foreach (var owner in owners)
                {
                    var exists = await _db.Notifications.AnyAsync(n =>
                        n.UserId == owner.Id &&
                        n.Type == "RentIncrease" &&
                        n.RelatedEntityId == contract.Id &&
                        n.CreatedAt > now.AddDays(-7));

                    if (!exists)
                    {
                        var newRent = contract.RentAmount * (1 + contract.AnnualIncreasePercent / 100);
                        await CreateAsync(
                            owner.Id,
                            "RentIncrease",
                            $"زيادة الإيجار مستحقة",
                            $"زيادة إيجار {contract.Tenant?.Name} للوحدة {contract.Unit?.UnitNumber}: من {contract.RentAmount:N0} ج.م إلى {newRent:N0} ج.م",
                            contract.Id, "Contract"
                        );
                    }
                }
            }
        }
    }
}
