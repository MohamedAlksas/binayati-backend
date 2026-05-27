using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BinayatiBackend.Models;

public class Floor
{
    [Key]
    public int Id { get; set; }

    public int BuildingId { get; set; }

    public int FloorNumber { get; set; }

    [MaxLength(200)]
    public string Label { get; set; } = string.Empty;

    [ForeignKey(nameof(BuildingId))]
    public Building? Building { get; set; }

    public List<Unit> Units { get; set; } = new();
}
