using Erp.Api.Data;
using Erp.Api.Dtos;
using Erp.Api.Models;
using Erp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public sealed class FinanceController(ErpDbContext db, FinancialEntryService financialEntryService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<FinancialSummaryDto>> GetSummary()
    {
        var entries = await IncludeEntryGraph(db.FinancialEntries.AsNoTracking())
            .Where(x => x.Status != FinancialEntryStatus.Cancelled)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var openPayables = entries.Where(x => x.Type == FinancialEntryType.Payable && x.Status == FinancialEntryStatus.Open).ToList();
        var openReceivables = entries.Where(x => x.Type == FinancialEntryType.Receivable && x.Status == FinancialEntryStatus.Open).ToList();
        var paidThisMonth = entries
            .Where(x => x.Type == FinancialEntryType.Payable && x.Status == FinancialEntryStatus.Paid && x.SettledAt >= monthStart && x.SettledAt < nextMonth)
            .Sum(x => x.PaidAmount);
        var receivedThisMonth = entries
            .Where(x => x.Type == FinancialEntryType.Receivable && x.Status == FinancialEntryStatus.Paid && x.SettledAt >= monthStart && x.SettledAt < nextMonth)
            .Sum(x => x.PaidAmount);

        return Ok(new FinancialSummaryDto(
            OpenPayables: openPayables.Sum(x => x.Amount - x.PaidAmount),
            OverduePayables: openPayables.Where(x => x.DueDate.Date < today).Sum(x => x.Amount - x.PaidAmount),
            OpenReceivables: openReceivables.Sum(x => x.Amount - x.PaidAmount),
            OverdueReceivables: openReceivables.Where(x => x.DueDate.Date < today).Sum(x => x.Amount - x.PaidAmount),
            PaidThisMonth: paidThisMonth,
            ReceivedThisMonth: receivedThisMonth,
            NetCashFlowThisMonth: receivedThisMonth - paidThisMonth,
            NextPayables: openPayables.OrderBy(x => x.DueDate).Take(5).Select(ToDto).ToList(),
            NextReceivables: openReceivables.OrderBy(x => x.DueDate).Take(5).Select(ToDto).ToList()));
    }

    [HttpGet("payables")]
    public Task<ActionResult<IReadOnlyCollection<FinancialEntryDto>>> GetPayables([FromQuery] FinancialEntryStatus? status)
    {
        return GetEntriesAsync(FinancialEntryType.Payable, status);
    }

    [HttpGet("receivables")]
    public Task<ActionResult<IReadOnlyCollection<FinancialEntryDto>>> GetReceivables([FromQuery] FinancialEntryStatus? status)
    {
        return GetEntriesAsync(FinancialEntryType.Receivable, status);
    }

    [HttpPost("entries")]
    public async Task<ActionResult<FinancialEntryDto>> CreateEntry(CreateFinancialEntryRequest request)
    {
        try
        {
            var entry = await financialEntryService.CreateManualAsync(
                request.Type,
                request.DueDate,
                request.Amount,
                request.Description,
                request.SupplierId,
                request.CustomerId,
                User.GetUserId(),
                HttpContext.RequestAborted);

            var saved = await GetEntryAsync(entry.Id);
            return CreatedAtAction(nameof(GetSummary), ToDto(saved));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("entries/{id:guid}/settle")]
    public async Task<ActionResult<FinancialEntryDto>> Settle(Guid id, SettleFinancialEntryRequest request)
    {
        try
        {
            var entry = await financialEntryService.SettleAsync(id, request.SettledAt, request.PaidAmount, User.GetUserId(), HttpContext.RequestAborted);
            if (entry is null)
            {
                return NotFound();
            }

            return Ok(ToDto(await GetEntryAsync(entry.Id)));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("entries/{id:guid}/cancel")]
    public async Task<ActionResult<FinancialEntryDto>> Cancel(Guid id)
    {
        try
        {
            var entry = await financialEntryService.CancelAsync(id, HttpContext.RequestAborted);
            if (entry is null)
            {
                return NotFound();
            }

            return Ok(ToDto(await GetEntryAsync(entry.Id)));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private async Task<ActionResult<IReadOnlyCollection<FinancialEntryDto>>> GetEntriesAsync(FinancialEntryType type, FinancialEntryStatus? status)
    {
        var query = IncludeEntryGraph(db.FinancialEntries.AsNoTracking())
            .Where(x => x.Type == type);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }
        else
        {
            query = query.Where(x => x.Status != FinancialEntryStatus.Cancelled);
        }

        var entries = await query
            .OrderBy(x => x.Status == FinancialEntryStatus.Open ? 0 : 1)
            .ThenBy(x => x.DueDate)
            .Take(250)
            .ToListAsync();

        return Ok(entries.Select(ToDto).ToList());
    }

    private async Task<FinancialEntry> GetEntryAsync(Guid id)
    {
        return await IncludeEntryGraph(db.FinancialEntries.AsNoTracking()).FirstAsync(x => x.Id == id);
    }

    private static IQueryable<FinancialEntry> IncludeEntryGraph(IQueryable<FinancialEntry> query)
    {
        return query
            .Include(x => x.Supplier)
            .Include(x => x.Customer)
            .Include(x => x.PurchaseOrder)
            .Include(x => x.SalesOrder);
    }

    private static FinancialEntryDto ToDto(FinancialEntry entry)
    {
        var openAmount = Math.Max(0, entry.Amount - entry.PaidAmount);

        return new FinancialEntryDto(
            entry.Id,
            entry.Number,
            entry.Type,
            entry.Status,
            entry.IssueDate,
            entry.DueDate,
            entry.SettledAt,
            entry.Amount,
            entry.PaidAmount,
            openAmount,
            entry.Status == FinancialEntryStatus.Open && entry.DueDate.Date < DateTime.UtcNow.Date,
            entry.Description,
            entry.SupplierId,
            entry.Supplier?.Name,
            entry.CustomerId,
            entry.Customer?.Name,
            entry.PurchaseOrderId,
            entry.PurchaseOrder?.Number,
            entry.SalesOrderId,
            entry.SalesOrder?.Number);
    }
}
