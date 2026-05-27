using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BinayatiBackend.Data;
using BinayatiBackend.DTOs;

namespace BinayatiBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db) => _db = db;

    [HttpGet("income")]
    public async Task<IActionResult> GetIncomeReport([FromQuery] int? year)
    {
        var now = DateTime.UtcNow;
        year ??= now.Year;

        var start = new DateTime(year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddYears(1);

        var monthlyData = await _db.Payments
            .Where(p => p.PaidDate >= start && p.PaidDate < end)
            .GroupBy(p => new { p.PaidDate.Year, p.PaidDate.Month })
            .Select(g => new MonthlyIncomeDto
            {
                Year = g.Key.Year,
                Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                Total = g.Sum(p => p.Amount),
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync();

        var totalIncome = monthlyData.Sum(m => m.Total);
        var avgMonthly = monthlyData.Count > 0 ? totalIncome / monthlyData.Count : 0;

        return Ok(new IncomeReportDto
        {
            MonthlyIncome = monthlyData,
            TotalIncome = totalIncome,
            AverageMonthlyIncome = avgMonthly,
        });
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdueReport()
    {
        var now = DateTime.UtcNow;
        var activeContracts = await _db.Contracts
            .Include(c => c.Tenant)
            .Include(c => c.Unit)
            .Include(c => c.Payments)
            .Where(c => c.Status == "Active")
            .ToListAsync();

        var overdueItems = new List<OverdueItemDto>();

        foreach (var contract in activeContracts)
        {
            var lastPayment = contract.Payments.OrderByDescending(p => p.PeriodEnd).FirstOrDefault();
            var lastPaidPeriod = lastPayment?.PeriodEnd ?? contract.StartDate.AddMonths(-1);
            var monthsSinceLastPayment = ((now.Year - lastPaidPeriod.Year) * 12) + (now.Month - lastPaidPeriod.Month);

            if (monthsSinceLastPayment > 1)
            {
                overdueItems.Add(new OverdueItemDto
                {
                    ContractId = contract.Id,
                    TenantName = contract.Tenant?.Name ?? "",
                    UnitNumber = contract.Unit?.UnitNumber ?? "",
                    RentAmount = contract.RentAmount,
                    MonthsOverdue = monthsSinceLastPayment - 1,
                    TotalDue = contract.RentAmount * (monthsSinceLastPayment - 1),
                    LastPaymentDate = lastPayment?.PaidDate ?? contract.StartDate,
                });
            }
        }

        return Ok(new OverdueReportDto
        {
            OverdueItems = overdueItems.OrderByDescending(o => o.TotalDue).ToList(),
            TotalOverdue = overdueItems.Count,
            TotalOverdueAmount = overdueItems.Sum(o => o.TotalDue),
        });
    }
}
