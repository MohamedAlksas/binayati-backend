using System.ComponentModel.DataAnnotations;

namespace BinayatiBackend.DTOs;

public class PaymentDto
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidDate { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class CreatePaymentRequest
{
    public int ContractId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    public DateTime PaidDate { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    [MaxLength(20)]
    public string Method { get; set; } = "Cash";

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;
}
