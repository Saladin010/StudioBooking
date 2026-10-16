using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioBooking.Domain.Entities;
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

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var room = new Room(Guid.NewGuid(), request.Name, request.Capacity, request.HourlyRate);

        _dbContext.Rooms.Add(room);
        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new RoomResponse(room.Id, room.Name, room.Capacity, room.HourlyRate));
    }
}

public record CreateRoomRequest(string Name, int Capacity, decimal HourlyRate);
public record RoomResponse(Guid Id, string Name, int Capacity, decimal HourlyRate);
