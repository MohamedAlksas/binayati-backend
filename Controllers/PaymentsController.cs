using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BinayatiBackend.Data;
using BinayatiBackend.DTOs;
using BinayatiBackend.Models;

namespace BinayatiBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PaymentsController(AppDbContext db) => _db = db;

    [HttpGet("contract/{contractId}")]
    public async Task<IActionResult> GetByContract(int contractId)
    {
        var payments = await _db.Payments
            .Where(p => p.ContractId == contractId)
            .OrderByDescending(p => p.PaidDate)
            .ToListAsync();

        var dtos = payments.Select(p => new PaymentDto
        {
            Id = p.Id,
            ContractId = p.ContractId,
            Amount = p.Amount,
            PaidDate = p.PaidDate,
            PeriodStart = p.PeriodStart,
            PeriodEnd = p.PeriodEnd,
            Method = p.Method,
            Notes = p.Notes,
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        var contract = await _db.Contracts.FindAsync(request.ContractId);
        if (contract == null) return NotFound(new { message = "Contract not found" });

        var payment = new Payment
        {
            ContractId = request.ContractId,
            Amount = request.Amount,
            PaidDate = request.PaidDate,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            Method = request.Method,
            Notes = request.Notes,
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return Ok(new { id = payment.Id });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var payment = await _db.Payments.FindAsync(id);
        if (payment == null) return NotFound(new { message = "Payment not found" });

        _db.Payments.Remove(payment);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
