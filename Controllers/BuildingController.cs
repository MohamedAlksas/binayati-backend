using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BinayatiBackend.Data;
using BinayatiBackend.DTOs;

namespace BinayatiBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BuildingController : ControllerBase
{
    private readonly AppDbContext _db;

    public BuildingController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetBuilding()
    {
        var building = await _db.Buildings
            .Include(b => b.Floors.OrderBy(f => f.FloorNumber))
                .ThenInclude(f => f.Units.OrderBy(u => u.UnitNumber))
            .FirstOrDefaultAsync();

        if (building == null)
            return Ok(new { });

        var dto = new BuildingDto
        {
            Id = building.Id,
            Name = building.Name,
            Address = building.Address,
            Floors = building.Floors.Select(f => new FloorDto
            {
                Id = f.Id,
                FloorNumber = f.FloorNumber,
                Label = f.Label,
                Units = f.Units.Select(u => new UnitDto
                {
                    Id = u.Id,
                    UnitNumber = u.UnitNumber,
                    Type = u.Type,
                    IsOwnerUnit = u.IsOwnerUnit,
                    Description = u.Description,
                    IsOccupied = u.IsOccupied,
                    FloorId = u.FloorId,
                }).ToList(),
            }).ToList(),
        };

        return Ok(dto);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateBuilding([FromBody] UpdateBuildingRequest request)
    {
        var building = await _db.Buildings.FirstOrDefaultAsync();
        if (building == null)
        {
            building = new Models.Building
            {
                Name = request.Name,
                Address = request.Address ?? "",
            };
            _db.Buildings.Add(building);
        }
        else
        {
            if (!string.IsNullOrEmpty(request.Name))
                building.Name = request.Name;
            if (request.Address != null)
                building.Address = request.Address;
        }

        await _db.SaveChangesAsync();
        return Ok(new { id = building.Id, message = "Building updated" });
    }
}

public class UpdateBuildingRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
}
