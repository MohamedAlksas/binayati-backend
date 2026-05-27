using System.ComponentModel.DataAnnotations;

namespace BinayatiBackend.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Role { get; set; } = "Owner";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
