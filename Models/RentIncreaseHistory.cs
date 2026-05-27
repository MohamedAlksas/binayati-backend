using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BinayatiBackend.Models;

public class RentIncreaseHistory
{
    [Key]
    public int Id { get; set; }

    public int ContractId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OldRent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NewRent { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal IncreasePercent { get; set; }

    public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ContractId))]
    public Contract? Contract { get; set; }
}
