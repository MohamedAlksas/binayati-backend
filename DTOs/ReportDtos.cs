namespace BinayatiBackend.DTOs;

public class IncomeReportDto
{
    public List<MonthlyIncomeDto> MonthlyIncome { get; set; } = new();
    public decimal TotalIncome { get; set; }
    public decimal AverageMonthlyIncome { get; set; }
}

public class OverdueReportDto
{
    public List<OverdueItemDto> OverdueItems { get; set; } = new();
    public int TotalOverdue { get; set; }
    public decimal TotalOverdueAmount { get; set; }
}

public class OverdueItemDto
{
    public int ContractId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public decimal RentAmount { get; set; }
    public int MonthsOverdue { get; set; }
    public decimal TotalDue { get; set; }
    public DateTime LastPaymentDate { get; set; }
}
