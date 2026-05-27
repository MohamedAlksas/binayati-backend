namespace BinayatiBackend.DTOs;

public class DashboardDto
{
    public decimal TotalMonthlyIncome { get; set; }
    public int ActiveContracts { get; set; }
    public int VacantUnits { get; set; }
    public int TotalUnits { get; set; }
    public decimal TotalSecurityDeposits { get; set; }
    public List<ContractSummaryDto> ExpiringSoon { get; set; } = new();
    public List<RecentPaymentDto> RecentPayments { get; set; } = new();
    public List<MonthlyIncomeDto> MonthlyIncome { get; set; } = new();
}

public class RecentPaymentDto
{
    public int Id { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaidDate { get; set; }
    public string Method { get; set; } = string.Empty;
}

public class MonthlyIncomeDto
{
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Total { get; set; }
}
