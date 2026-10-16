using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudioBooking.Api.Controllers;
using StudioBooking.Domain.Entities;
using StudioBooking.Infrastructure.Persistence;

namespace StudioBooking.Api.IntegrationTests;

public class RoomsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RoomsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRooms_WhenNoRoomsExist_Returns200OkWithEmptyList()
    {
        // Arrange: Ensure Rooms table is empty
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Rooms.RemoveRange(dbContext.Rooms);
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/rooms");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rooms = await response.Content.ReadFromJsonAsync<List<RoomResponse>>();
        rooms.Should().NotBeNull();
        rooms.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRooms_WhenRoomsExist_Returns200OkWithAllRooms()
    {
        // Arrange: Insert two rooms into the database via DbContext
        var room1 = new Room(Guid.NewGuid(), "Studio A - Control Room", 6, 120.00m);
        var room2 = new Room(Guid.NewGuid(), "Studio B - Live Room", 12, 200.50m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Rooms.RemoveRange(dbContext.Rooms);
            await dbContext.SaveChangesAsync();

            dbContext.Rooms.AddRange(room1, room2);
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/rooms");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rooms = await response.Content.ReadFromJsonAsync<List<RoomResponse>>();
        rooms.Should().NotBeNull();
        rooms.Should().HaveCount(2);

        var retrievedRoom1 = rooms!.FirstOrDefault(r => r.Id == room1.Id);
        retrievedRoom1.Should().NotBeNull();
        retrievedRoom1!.Name.Should().Be(room1.Name);
        retrievedRoom1.Capacity.Should().Be(room1.Capacity);
        retrievedRoom1.HourlyRate.Should().Be(room1.HourlyRate);

        var retrievedRoom2 = rooms!.FirstOrDefault(r => r.Id == room2.Id);
        retrievedRoom2.Should().NotBeNull();
        retrievedRoom2!.Name.Should().Be(room2.Name);
        retrievedRoom2.Capacity.Should().Be(room2.Capacity);
        retrievedRoom2.HourlyRate.Should().Be(room2.HourlyRate);
    }
}
