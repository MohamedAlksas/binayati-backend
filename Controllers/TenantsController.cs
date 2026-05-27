using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BinayatiBackend.Data;
using BinayatiBackend.DTOs;

namespace BinayatiBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TenantsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenants = await _db.Tenants
            .Include(t => t.Contracts.Where(c => c.Status == "Active"))
            .OrderBy(t => t.Name)
            .ToListAsync();

        var dtos = tenants.Select(t => new TenantDto
        {
            Id = t.Id,
            Name = t.Name,
            PhoneNumber = t.PhoneNumber,
            Email = t.Email,
            NationalId = t.NationalId,
            Notes = t.Notes,
            CreatedAt = t.CreatedAt,
            Contracts = t.Contracts.Select(c => new ContractSummaryDto
            {
                Id = c.Id,
                TenantName = t.Name,
                UnitNumber = c.Unit?.UnitNumber ?? "",
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                RentAmount = c.RentAmount,
                Status = c.Status,
                DaysUntilExpiry = (c.EndDate - DateTime.UtcNow).Days,
            }).ToList(),
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tenant = await _db.Tenants
            .Include(t => t.Contracts.OrderByDescending(c => c.StartDate))
                .ThenInclude(c => c.Unit)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null) return NotFound(new { message = "Tenant not found" });

        var dto = new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            PhoneNumber = tenant.PhoneNumber,
            Email = tenant.Email,
            NationalId = tenant.NationalId,
            Notes = tenant.Notes,
            CreatedAt = tenant.CreatedAt,
            Contracts = tenant.Contracts.Select(c => new ContractSummaryDto
            {
                Id = c.Id,
                TenantName = tenant.Name,
                UnitNumber = c.Unit?.UnitNumber ?? "",
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                RentAmount = c.RentAmount,
                Status = c.Status,
                DaysUntilExpiry = (c.EndDate - DateTime.UtcNow).Days,
            }).ToList(),
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request)
    {
        if (!string.IsNullOrEmpty(request.NationalId) &&
            await _db.Tenants.AnyAsync(t => t.NationalId == request.NationalId))
            return BadRequest(new { message = "National ID already exists" });

        var tenant = new Models.Tenant
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            NationalId = request.NationalId,
            Notes = request.Notes,
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, new { id = tenant.Id, name = tenant.Name });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTenantRequest request)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound(new { message = "Tenant not found" });

        if (request.Name != null) tenant.Name = request.Name;
        if (request.PhoneNumber != null) tenant.PhoneNumber = request.PhoneNumber;
        if (request.Email != null) tenant.Email = request.Email;
        if (request.NationalId != null) tenant.NationalId = request.NationalId;
        if (request.Notes != null) tenant.Notes = request.Notes;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Tenant updated" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tenant = await _db.Tenants.Include(t => t.Contracts).FirstOrDefaultAsync(t => t.Id == id);
        if (tenant == null) return NotFound(new { message = "Tenant not found" });
        if (tenant.Contracts.Any(c => c.Status == "Active"))
            return Conflict(new { message = "Cannot delete tenant with active contracts" });

        _db.Tenants.Remove(tenant);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
