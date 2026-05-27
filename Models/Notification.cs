using System.ComponentModel.DataAnnotations;

namespace BinayatiBackend.Models;

public class Notification
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public int? RelatedEntityId { get; set; }

    [MaxLength(50)]
    public string RelatedEntityType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
