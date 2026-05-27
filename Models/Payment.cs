using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BinayatiBackend.Models;

public class Payment
{
    [Key]
    public int Id { get; set; }

    public int ContractId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime PaidDate { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    [MaxLength(20)]
    public string Method { get; set; } = "Cash";

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    [ForeignKey(nameof(ContractId))]
    public Contract? Contract { get; set; }
}
