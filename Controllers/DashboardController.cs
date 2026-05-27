using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BinayatiBackend.Data;
using BinayatiBackend.DTOs;

namespace BinayatiBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var activeContracts = await _db.Contracts
            .Include(c => c.Tenant)
            .Include(c => c.Unit)
            .Where(c => c.Status == "Active")
            .ToListAsync();

        var totalUnits = await _db.Units.CountAsync();
        var occupiedUnits = activeContracts.Select(c => c.UnitId).Distinct().Count();
        var ownerUnits = await _db.Units.CountAsync(u => u.IsOwnerUnit);

        var totalMonthlyIncome = activeContracts.Sum(c => c.RentAmount);

        var monthPayments = await _db.Payments
            .Where(p => p.PaidDate >= monthStart)
            .SumAsync(p => p.Amount);

        var recentPayments = await _db.Payments
            .Include(p => p.Contract).ThenInclude(c => c!.Tenant)
            .Include(p => p.Contract).ThenInclude(c => c!.Unit)
            .OrderByDescending(p => p.PaidDate)
            .Take(10)
            .ToListAsync();

        var expiringSoon = activeContracts
            .Where(c => c.EndDate <= now.AddDays(30) && c.EndDate >= now)
            .OrderBy(c => c.EndDate)
            .Select(c => new ContractSummaryDto
            {
                Id = c.Id,
                TenantName = c.Tenant?.Name ?? "",
                UnitNumber = c.Unit?.UnitNumber ?? "",
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                RentAmount = c.RentAmount,
                Status = c.Status,
                DaysUntilExpiry = (c.EndDate - now).Days,
            })
            .ToList();

        var last12Months = Enumerable.Range(0, 12).Select(i =>
        {
            var d = now.AddMonths(-i);
            return new { Year = d.Year, Month = d.Month };
        }).ToList();

        var monthlyIncome = new List<MonthlyIncomeDto>();
        foreach (var m in last12Months)
        {
            var start = new DateTime(m.Year, m.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);
            var total = await _db.Payments
                .Where(p => p.PaidDate >= start && p.PaidDate < end)
                .SumAsync(p => p.Amount);

            monthlyIncome.Add(new MonthlyIncomeDto
            {
                Year = m.Year,
                Month = new DateTime(m.Year, m.Month, 1).ToString("MMM"),
                Total = total,
            });
        }
        monthlyIncome.Reverse();

        var totalDeposits = activeContracts.Sum(c => c.SecurityDeposit);

        var dto = new DashboardDto
        {
            TotalMonthlyIncome = totalMonthlyIncome,
            ActiveContracts = activeContracts.Count,
            VacantUnits = totalUnits - occupiedUnits - ownerUnits,
            TotalUnits = totalUnits,
            TotalSecurityDeposits = totalDeposits,
            ExpiringSoon = expiringSoon,
            RecentPayments = recentPayments.Select(p => new RecentPaymentDto
            {
                Id = p.Id,
                TenantName = p.Contract?.Tenant?.Name ?? "",
                UnitNumber = p.Contract?.Unit?.UnitNumber ?? "",
                Amount = p.Amount,
                PaidDate = p.PaidDate,
                Method = p.Method,
            }).ToList(),
            MonthlyIncome = monthlyIncome,
        };

        return Ok(dto);
    }
}
