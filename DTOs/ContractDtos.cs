using System.ComponentModel.DataAnnotations;

namespace BinayatiBackend.DTOs;

public class ContractSummaryDto
{
    public int Id { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysUntilExpiry { get; set; }
}

public class ContractDto
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentAmount { get; set; }
    public decimal AnnualIncreasePercent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public bool DepositRefunded { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal NextRent { get; set; }
    public DateTime? NextIncreaseDate { get; set; }
    public List<PaymentDto> Payments { get; set; } = new();
    public List<RentIncreaseHistoryDto> RentIncreaseHistories { get; set; } = new();
}

public class CreateContractRequest
{
    public int UnitId { get; set; }
    public int TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal RentAmount { get; set; }

    [Range(0, 100)]
    public decimal AnnualIncreasePercent { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SecurityDeposit { get; set; }

    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;
}

public class UpdateContractRequest
{
    public DateTime? EndDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? RentAmount { get; set; }

    [Range(0, 100)]
    public decimal? AnnualIncreasePercent { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool? DepositRefunded { get; set; }
}

public class ContractFilterRequest
{
    public string? Status { get; set; }
    public int? UnitId { get; set; }
    public int? TenantId { get; set; }
    public bool? ExpiringSoon { get; set; }
}
