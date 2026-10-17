using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StudioBooking.Domain.Entities;
using StudioBooking.Infrastructure.Persistence;

namespace StudioBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class BookingsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public BookingsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("me")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyBookings()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User identifier is missing or invalid." });
        }

        var bookings = await _dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Room)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.StartTime)
            .ToListAsync();

        var response = bookings.Select(b => new BookingResponse(
            b.Id,
            b.RoomId,
            b.UserId,
            b.StartTime,
            b.EndTime,
            b.Status,
            b.Room?.Name ?? string.Empty
        )).ToList();

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User identifier is missing or invalid." });
        }

        var booking = new Booking(
            Guid.NewGuid(),
            request.RoomId,
            userId,
            request.StartTime,
            request.EndTime,
            "Active"
        );

        _dbContext.Bookings.Add(booking);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (
            (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23P01") ||
            (ex.GetBaseException() is PostgresException basePgEx && basePgEx.SqlState == "23P01"))
        {
            return Conflict(new { message = "The room is already booked for the selected time slot." });
        }

        var response = new BookingResponse(
            booking.Id,
            booking.RoomId,
            booking.UserId,
            booking.StartTime,
            booking.EndTime,
            booking.Status
        );

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> CancelBooking([FromRoute] Guid id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User identifier is missing or invalid." });
        }

        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == id);
        if (booking is null)
        {
            return NotFound(new { message = "Booking not found." });
        }

        if (booking.UserId != userId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to cancel this booking." });
        }

        booking.Status = "Cancelled";
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateBookingRequest(Guid RoomId, DateTimeOffset StartTime, DateTimeOffset EndTime);
public record BookingResponse(Guid Id, Guid RoomId, Guid UserId, DateTimeOffset StartTime, DateTimeOffset EndTime, string Status, string? RoomName = null);
