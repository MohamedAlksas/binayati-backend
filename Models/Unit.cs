using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BinayatiBackend.Models;

public class Unit
{
    [Key]
    public int Id { get; set; }

    public int FloorId { get; set; }

    [Required, MaxLength(50)]
    public string UnitNumber { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Type { get; set; } = "Apartment";

    public bool IsOwnerUnit { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsOccupied { get; set; }

    [ForeignKey(nameof(FloorId))]
    public Floor? Floor { get; set; }

    public List<Contract> Contracts { get; set; } = new();
    public List<MaintenanceRequest> MaintenanceRequests { get; set; } = new();
}
