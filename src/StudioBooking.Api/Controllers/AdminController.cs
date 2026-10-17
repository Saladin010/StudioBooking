using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioBooking.Infrastructure.Persistence;

namespace StudioBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("dashboard")]
    public IActionResult GetDashboard()
    {
        return Ok(new { message = "Admin Access Granted" });
    }

    [HttpPatch("disputes/{id:guid}")]
    public async Task<IActionResult> ResolveDispute([FromRoute] Guid id, [FromBody] ResolveDisputeRequest request)
    {
        var dispute = await _dbContext.Disputes.FirstOrDefaultAsync(d => d.Id == id);
        if (dispute is null)
        {
            return NotFound(new { message = "Dispute not found." });
        }

        dispute.Status = request.ResolutionStatus;
        if (request.ResolutionNotes is not null)
        {
            dispute.ResolutionNotes = request.ResolutionNotes;
        }

        await _dbContext.SaveChangesAsync();

        var response = new DisputeResponse(
            dispute.Id,
            dispute.BookingId,
            dispute.CustomerId,
            dispute.Reason,
            dispute.Status,
            dispute.CustomerId,
            dispute.ResolutionNotes
        );

        return Ok(response);
    }
}

public record ResolveDisputeRequest(string ResolutionStatus, string? ResolutionNotes = null);
