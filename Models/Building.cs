using System.ComponentModel.DataAnnotations;

namespace BinayatiBackend.Models;

public class Building
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Floor> Floors { get; set; } = new();
}
