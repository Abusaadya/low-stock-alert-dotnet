using Microsoft.AspNetCore.Mvc;
using SallaAlertApp.Api.Services;

namespace SallaAlertApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Test endpoint to manually trigger weekly report for a merchant
    /// </summary>
    [HttpPost("test-weekly/{merchantId}")]
    public async Task<IActionResult> TestWeeklyReport(long merchantId)
    {
        try
        {
            await _reportService.SendWeeklyReport(merchantId);
            return Ok(new { message = "Weekly report sent successfully", merchantId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test endpoint to manually trigger monthly report for a merchant
    /// </summary>
    [HttpPost("test-monthly/{merchantId}")]
    public async Task<IActionResult> TestMonthlyReport(long merchantId)
    {
        try
        {
            await _reportService.SendMonthlyReport(merchantId);
            return Ok(new { message = "Monthly report sent successfully", merchantId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
