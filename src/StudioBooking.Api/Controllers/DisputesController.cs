using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioBooking.Domain.Entities;
using StudioBooking.Infrastructure.Persistence;

namespace StudioBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class DisputesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public DisputesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDispute([FromBody] CreateDisputeRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User identifier is missing or invalid." });
        }

        if (request.BookingId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "BookingId and Reason are required." });
        }

        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == request.BookingId);
        if (booking is null)
        {
            return NotFound(new { message = "Booking not found." });
        }

        if (booking.UserId != userId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to dispute this booking." });
        }

        var dispute = new Dispute(
            Guid.NewGuid(),
            request.BookingId,
            userId,
            request.Reason,
            "Pending"
        );

        _dbContext.Disputes.Add(dispute);
        await _dbContext.SaveChangesAsync();

        var response = new DisputeResponse(
            dispute.Id,
            dispute.BookingId,
            dispute.CustomerId,
            dispute.Reason,
            dispute.Status,
            dispute.CustomerId
        );

        return StatusCode(StatusCodes.Status201Created, response);
    }
}

public record CreateDisputeRequest(Guid BookingId, string Reason);

public record DisputeResponse(
    Guid Id,
    Guid BookingId,
    Guid CustomerId,
    string Reason,
    string Status,
    Guid? UserId = null,
    string? ResolutionNotes = null);
