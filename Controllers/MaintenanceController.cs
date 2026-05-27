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
public class MaintenanceController : ControllerBase
{
    private readonly AppDbContext _db;

    public MaintenanceController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var query = _db.MaintenanceRequests
            .Include(m => m.Unit)
            .Include(m => m.Tenant)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(m => m.Status == status);

        var requests = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();

        var dtos = requests.Select(m => new MaintenanceRequestDto
        {
            Id = m.Id,
            UnitId = m.UnitId,
            UnitNumber = m.Unit?.UnitNumber ?? "",
            TenantId = m.TenantId,
            TenantName = m.Tenant?.Name ?? "",
            Title = m.Title,
            Description = m.Description,
            Status = m.Status,
            CreatedAt = m.CreatedAt,
            ResolvedAt = m.ResolvedAt,
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMaintenanceRequest request)
    {
        var unit = await _db.Units.FindAsync(request.UnitId);
        if (unit == null) return BadRequest(new { message = "Unit not found" });

        var requestEntity = new MaintenanceRequest
        {
            UnitId = request.UnitId,
            TenantId = request.TenantId,
            Title = request.Title,
            Description = request.Description,
        };

        _db.MaintenanceRequests.Add(requestEntity);
        await _db.SaveChangesAsync();

        return Ok(new { id = requestEntity.Id });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateMaintenanceStatusRequest request)
    {
        var maintenance = await _db.MaintenanceRequests.FindAsync(id);
        if (maintenance == null) return NotFound(new { message = "Request not found" });

        maintenance.Status = request.Status;
        if (request.Status == "Completed" || request.Status == "Resolved")
            maintenance.ResolvedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Status updated" });
    }
}
