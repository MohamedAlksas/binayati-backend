using System.ComponentModel.DataAnnotations;

namespace BinayatiBackend.DTOs;

public class MaintenanceRequestDto
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public int? TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class CreateMaintenanceRequest
{
    public int UnitId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public int? TenantId { get; set; }
}

public class UpdateMaintenanceStatusRequest
{
    [Required, MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}
