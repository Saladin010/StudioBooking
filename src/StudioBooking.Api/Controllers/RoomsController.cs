using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioBooking.Infrastructure.Persistence;

namespace StudioBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public RoomsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRooms()
    {
        var rooms = await _dbContext.Rooms
            .AsNoTracking()
            .Select(r => new RoomResponse(r.Id, r.Name, r.Capacity, r.HourlyRate))
            .ToListAsync();

        return Ok(rooms);
    }
}

public record RoomResponse(Guid Id, string Name, int Capacity, decimal HourlyRate);
