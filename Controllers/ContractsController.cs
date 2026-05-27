using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BinayatiBackend.Data;
using BinayatiBackend.DTOs;
using BinayatiBackend.Models;

namespace BinayatiBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContractsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ContractsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] bool? expiringSoon)
    {
        var query = _db.Contracts
            .Include(c => c.Unit)
            .Include(c => c.Tenant)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        if (expiringSoon == true)
        {
            var soon = DateTime.UtcNow.AddDays(30);
            query = query.Where(c => c.Status == "Active" && c.EndDate <= soon && c.EndDate >= DateTime.UtcNow);
        }

        var contracts = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

        var dtos = contracts.Select(c => new ContractSummaryDto
        {
            Id = c.Id,
            TenantName = c.Tenant?.Name ?? "",
            UnitNumber = c.Unit?.UnitNumber ?? "",
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            RentAmount = c.RentAmount,
            Status = c.Status,
            DaysUntilExpiry = (c.EndDate - DateTime.UtcNow).Days,
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contract = await _db.Contracts
            .Include(c => c.Unit)
            .Include(c => c.Tenant)
            .Include(c => c.Payments.OrderByDescending(p => p.PaidDate))
            .Include(c => c.RentIncreaseHistories.OrderByDescending(r => r.AppliedDate))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract == null) return NotFound(new { message = "Contract not found" });

        var lastIncrease = contract.RentIncreaseHistories.FirstOrDefault();
        var lastIncreaseDate = lastIncrease?.AppliedDate ?? contract.StartDate;
        var nextIncreaseDate = lastIncreaseDate.AddYears(1);
        var nextRent = contract.RentAmount * (1 + contract.AnnualIncreasePercent / 100);

        var dto = new ContractDto
        {
            Id = contract.Id,
            UnitId = contract.UnitId,
            UnitNumber = contract.Unit?.UnitNumber ?? "",
            UnitType = contract.Unit?.Type ?? "",
            TenantId = contract.TenantId,
            TenantName = contract.Tenant?.Name ?? "",
            TenantPhone = contract.Tenant?.PhoneNumber ?? "",
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            RentAmount = contract.RentAmount,
            AnnualIncreasePercent = contract.AnnualIncreasePercent,
            SecurityDeposit = contract.SecurityDeposit,
            DepositRefunded = contract.DepositRefunded,
            Status = contract.Status,
            Notes = contract.Notes,
            CreatedAt = contract.CreatedAt,
            NextRent = nextRent,
            NextIncreaseDate = nextIncreaseDate > contract.EndDate ? null : nextIncreaseDate,
            Payments = contract.Payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                ContractId = p.ContractId,
                Amount = p.Amount,
                PaidDate = p.PaidDate,
                PeriodStart = p.PeriodStart,
                PeriodEnd = p.PeriodEnd,
                Method = p.Method,
                Notes = p.Notes,
            }).ToList(),
            RentIncreaseHistories = contract.RentIncreaseHistories.Select(r => new RentIncreaseHistoryDto
            {
                Id = r.Id,
                ContractId = r.ContractId,
                OldRent = r.OldRent,
                NewRent = r.NewRent,
                IncreasePercent = r.IncreasePercent,
                AppliedDate = r.AppliedDate,
            }).ToList(),
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContractRequest request)
    {
        var unit = await _db.Units.FindAsync(request.UnitId);
        if (unit == null) return BadRequest(new { message = "Unit not found" });

        var tenant = await _db.Tenants.FindAsync(request.TenantId);
        if (tenant == null) return BadRequest(new { message = "Tenant not found" });

        if (await _db.Contracts.AnyAsync(c => c.UnitId == request.UnitId && c.Status == "Active"))
            return BadRequest(new { message = "Unit already has an active contract" });

        var contract = new Contract
        {
            UnitId = request.UnitId,
            TenantId = request.TenantId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            RentAmount = request.RentAmount,
            AnnualIncreasePercent = request.AnnualIncreasePercent,
            SecurityDeposit = request.SecurityDeposit,
            Notes = request.Notes,
            Status = "Active",
        };

        _db.Contracts.Add(contract);
        unit.IsOccupied = true;
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = contract.Id }, new { id = contract.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContractRequest request)
    {
        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound(new { message = "Contract not found" });

        if (request.EndDate.HasValue) contract.EndDate = request.EndDate.Value;
        if (request.RentAmount.HasValue) contract.RentAmount = request.RentAmount.Value;
        if (request.AnnualIncreasePercent.HasValue) contract.AnnualIncreasePercent = request.AnnualIncreasePercent.Value;
        if (request.Notes != null) contract.Notes = request.Notes;
        if (request.DepositRefunded.HasValue) contract.DepositRefunded = request.DepositRefunded.Value;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Contract updated" });
    }

    [HttpPut("{id}/terminate")]
    public async Task<IActionResult> Terminate(int id)
    {
        var contract = await _db.Contracts.Include(c => c.Unit).FirstOrDefaultAsync(c => c.Id == id);
        if (contract == null) return NotFound(new { message = "Contract not found" });

        contract.Status = "Terminated";
        if (contract.Unit != null)
            contract.Unit.IsOccupied = false;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Contract terminated" });
    }

    [HttpPost("{id}/apply-increase")]
    public async Task<IActionResult> ApplyIncrease(int id, [FromBody] ApplyRentIncreaseRequest request)
    {
        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound(new { message = "Contract not found" });
        if (contract.Status != "Active")
            return BadRequest(new { message = "Contract is not active" });

        var increasePercent = request.IncreasePercent > 0 ? request.IncreasePercent : contract.AnnualIncreasePercent;
        var oldRent = contract.RentAmount;
        var newRent = oldRent * (1 + increasePercent / 100);

        _db.RentIncreaseHistories.Add(new RentIncreaseHistory
        {
            ContractId = id,
            OldRent = oldRent,
            NewRent = newRent,
            IncreasePercent = increasePercent,
            AppliedDate = DateTime.UtcNow,
        });

        contract.RentAmount = newRent;

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = "RentIncreaseApplied",
            Title = "تم تطبيق زيادة الإيجار",
            Message = $"تم زيادة إيجار {contract.Tenant?.Name} من {oldRent:N0} ج.م إلى {newRent:N0} ج.م",
            RelatedEntityId = contract.Id,
            RelatedEntityType = "Contract",
        });

        await _db.SaveChangesAsync();
        return Ok(new { oldRent, newRent, increasePercent });
    }
}
