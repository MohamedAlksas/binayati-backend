namespace BinayatiBackend.DTOs;

public class RentIncreaseHistoryDto
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public decimal OldRent { get; set; }
    public decimal NewRent { get; set; }
    public decimal IncreasePercent { get; set; }
    public DateTime AppliedDate { get; set; }
}

public class ApplyRentIncreaseRequest
{
    public decimal IncreasePercent { get; set; }
}
