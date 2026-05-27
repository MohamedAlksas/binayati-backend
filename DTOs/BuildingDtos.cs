using System.ComponentModel.DataAnnotations;

namespace BinayatiBackend.DTOs;

public class BuildingDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<FloorDto> Floors { get; set; } = new();
}

public class FloorDto
{
    public int Id { get; set; }
    public int FloorNumber { get; set; }
    public string Label { get; set; } = string.Empty;
    public List<UnitDto> Units { get; set; } = new();
}

public class UnitDto
{
    public int Id { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsOwnerUnit { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
    public int FloorId { get; set; }
    public ContractSummaryDto? ActiveContract { get; set; }
}

public class UnitDetailDto
{
    public int Id { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsOwnerUnit { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
    public int FloorId { get; set; }
    public string FloorLabel { get; set; } = string.Empty;
    public ContractDto? ActiveContract { get; set; }
    public List<ContractSummaryDto> Contracts { get; set; } = new();
    public List<MaintenanceRequestDto> MaintenanceRequests { get; set; } = new();
}

public class CreateUnitRequest
{
    [Required, MaxLength(50)]
    public string UnitNumber { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Type { get; set; } = "Apartment";

    public bool IsOwnerUnit { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public int FloorId { get; set; }
}

public class UpdateUnitRequest
{
    [MaxLength(50)]
    public string? UnitNumber { get; set; }

    [MaxLength(20)]
    public string? Type { get; set; }

    public bool? IsOwnerUnit { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
