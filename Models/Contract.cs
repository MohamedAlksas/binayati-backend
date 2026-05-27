using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BinayatiBackend.Models;

public class Contract
{
    [Key]
    public int Id { get; set; }

    public int UnitId { get; set; }

    public int TenantId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RentAmount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal AnnualIncreasePercent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SecurityDeposit { get; set; }

    public bool DepositRefunded { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UnitId))]
    public Unit? Unit { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    public List<Payment> Payments { get; set; } = new();
    public List<RentIncreaseHistory> RentIncreaseHistories { get; set; } = new();
}
