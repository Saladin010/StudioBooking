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
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public ReviewsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User identifier is missing or invalid." });
        }

        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest(new { message = "Rating must be between 1 and 5." });
        }

        var roomExists = await _dbContext.Rooms.AnyAsync(r => r.Id == request.RoomId);
        if (!roomExists)
        {
            return NotFound(new { message = "Room not found." });
        }

        var review = new Review(
            Guid.NewGuid(),
            request.RoomId,
            userId,
            request.Rating,
            request.Comment,
            DateTimeOffset.UtcNow
        );

        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync();

        var response = new ReviewResponse(
            review.Id,
            review.RoomId,
            review.UserId,
            review.Rating,
            review.Comment,
            review.CreatedAt
        );

        return StatusCode(StatusCodes.Status201Created, response);
    }
}

public record CreateReviewRequest(Guid RoomId, int Rating, string Comment);

public record ReviewResponse(
    Guid Id,
    Guid RoomId,
    Guid UserId,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt,
    string? UserEmail = null,
    string? Email = null
);
