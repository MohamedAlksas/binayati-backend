using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BinayatiBackend.Data;
using BinayatiBackend.DTOs;

namespace BinayatiBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UnitsController : ControllerBase
{
    private readonly AppDbContext _db;

    public UnitsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var units = await _db.Units
            .Include(u => u.Floor)
            .Include(u => u.Contracts.Where(c => c.Status == "Active"))
            .OrderBy(u => u.Floor!.FloorNumber).ThenBy(u => u.UnitNumber)
            .ToListAsync();

        var dtos = units.Select(u => new UnitDto
        {
            Id = u.Id,
            UnitNumber = u.UnitNumber,
            Type = u.Type,
            IsOwnerUnit = u.IsOwnerUnit,
            Description = u.Description,
            IsOccupied = u.IsOccupied,
            FloorId = u.FloorId,
            ActiveContract = u.Contracts.FirstOrDefault() != null ? new ContractSummaryDto
            {
                Id = u.Contracts.First().Id,
                TenantName = u.Contracts.First().Tenant?.Name ?? "",
                UnitNumber = u.UnitNumber,
                StartDate = u.Contracts.First().StartDate,
                EndDate = u.Contracts.First().EndDate,
                RentAmount = u.Contracts.First().RentAmount,
                Status = u.Contracts.First().Status,
                DaysUntilExpiry = (u.Contracts.First().EndDate - DateTime.UtcNow).Days,
            } : null,
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var unit = await _db.Units
            .Include(u => u.Floor)
            .Include(u => u.Contracts.OrderByDescending(c => c.StartDate))
                .ThenInclude(c => c.Tenant)
            .Include(u => u.Contracts.OrderByDescending(c => c.StartDate))
                .ThenInclude(c => c.Payments.OrderByDescending(p => p.PaidDate))
            .Include(u => u.MaintenanceRequests.OrderByDescending(m => m.CreatedAt))
                .ThenInclude(m => m.Tenant)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (unit == null) return NotFound(new { message = "Unit not found" });

        var activeContract = unit.Contracts.FirstOrDefault(c => c.Status == "Active");

        var dto = new UnitDetailDto
        {
            Id = unit.Id,
            UnitNumber = unit.UnitNumber,
            Type = unit.Type,
            IsOwnerUnit = unit.IsOwnerUnit,
            Description = unit.Description,
            IsOccupied = unit.IsOccupied,
            FloorId = unit.FloorId,
            FloorLabel = unit.Floor?.Label ?? $"Floor {unit.Floor?.FloorNumber}",
            ActiveContract = activeContract != null ? MapContract(activeContract) : null,
            Contracts = unit.Contracts.Select(c => new ContractSummaryDto
            {
                Id = c.Id,
                TenantName = c.Tenant?.Name ?? "",
                UnitNumber = unit.UnitNumber,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                RentAmount = c.RentAmount,
                Status = c.Status,
                DaysUntilExpiry = (c.EndDate - DateTime.UtcNow).Days,
            }).ToList(),
            MaintenanceRequests = unit.MaintenanceRequests.Select(m => new MaintenanceRequestDto
            {
                Id = m.Id,
                UnitId = m.UnitId,
                UnitNumber = unit.UnitNumber,
                TenantId = m.TenantId,
                TenantName = m.Tenant?.Name ?? "",
                Title = m.Title,
                Description = m.Description,
                Status = m.Status,
                CreatedAt = m.CreatedAt,
                ResolvedAt = m.ResolvedAt,
            }).ToList(),
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnitRequest request)
    {
        var floor = await _db.Floors.FindAsync(request.FloorId);
        if (floor == null)
            return BadRequest(new { message = "Floor not found" });

        var unit = new Models.Unit
        {
            FloorId = request.FloorId,
            UnitNumber = request.UnitNumber,
            Type = request.Type,
            IsOwnerUnit = request.IsOwnerUnit,
            Description = request.Description,
        };

        _db.Units.Add(unit);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = unit.Id }, new { id = unit.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUnitRequest request)
    {
        var unit = await _db.Units.FindAsync(id);
        if (unit == null) return NotFound(new { message = "Unit not found" });

        if (request.UnitNumber != null) unit.UnitNumber = request.UnitNumber;
        if (request.Type != null) unit.Type = request.Type;
        if (request.IsOwnerUnit.HasValue) unit.IsOwnerUnit = request.IsOwnerUnit.Value;
        if (request.Description != null) unit.Description = request.Description;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Unit updated" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var unit = await _db.Units.Include(u => u.Contracts).FirstOrDefaultAsync(u => u.Id == id);
        if (unit == null) return NotFound(new { message = "Unit not found" });
        if (unit.Contracts.Any(c => c.Status == "Active"))
            return Conflict(new { message = "Cannot delete unit with active contracts" });

        _db.Units.Remove(unit);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private ContractDto MapContract(Models.Contract c)
    {
        var lastIncrease = c.RentIncreaseHistories?.OrderByDescending(r => r.AppliedDate).FirstOrDefault();
        var lastIncreaseDate = lastIncrease?.AppliedDate ?? c.StartDate;
        var nextIncreaseDate = lastIncreaseDate.AddYears(1);
        var nextRent = c.RentAmount * (1 + c.AnnualIncreasePercent / 100);

        return new ContractDto
        {
            Id = c.Id,
            UnitId = c.UnitId,
            UnitNumber = c.Unit?.UnitNumber ?? "",
            UnitType = c.Unit?.Type ?? "",
            TenantId = c.TenantId,
            TenantName = c.Tenant?.Name ?? "",
            TenantPhone = c.Tenant?.PhoneNumber ?? "",
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            RentAmount = c.RentAmount,
            AnnualIncreasePercent = c.AnnualIncreasePercent,
            SecurityDeposit = c.SecurityDeposit,
            DepositRefunded = c.DepositRefunded,
            Status = c.Status,
            Notes = c.Notes,
            CreatedAt = c.CreatedAt,
            NextRent = nextRent,
            NextIncreaseDate = nextIncreaseDate > c.EndDate ? null : nextIncreaseDate,
            Payments = c.Payments?.Select(p => new PaymentDto
            {
                Id = p.Id,
                ContractId = p.ContractId,
                Amount = p.Amount,
                PaidDate = p.PaidDate,
                PeriodStart = p.PeriodStart,
                PeriodEnd = p.PeriodEnd,
                Method = p.Method,
                Notes = p.Notes,
            }).ToList() ?? new(),
            RentIncreaseHistories = c.RentIncreaseHistories?.Select(r => new RentIncreaseHistoryDto
            {
                Id = r.Id,
                ContractId = r.ContractId,
                OldRent = r.OldRent,
                NewRent = r.NewRent,
                IncreasePercent = r.IncreasePercent,
                AppliedDate = r.AppliedDate,
            }).ToList() ?? new(),
        };
    }
}
