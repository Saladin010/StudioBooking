using FluentAssertions;
using StudioBooking.Domain.Entities;

namespace StudioBooking.Domain.UnitTests;

public class BookingTests
{
    [Fact]
    public void Constructor_WithValidArguments_InitializesPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddHours(2);
        const string status = "Active";

        // Act
        var booking = new Booking(id, roomId, userId, start, end, status);

        // Assert
        booking.Id.Should().Be(id);
        booking.RoomId.Should().Be(roomId);
        booking.UserId.Should().Be(userId);
        booking.StartTime.Should().Be(start);
        booking.EndTime.Should().Be(end);
        booking.Status.Should().Be(status);
    }

    [Fact]
    public void ParameterlessConstructor_InitializesEmptyDefaults()
    {
        // Act
        var booking = new Booking();

        // Assert
        booking.Id.Should().BeEmpty();
        booking.RoomId.Should().BeEmpty();
        booking.UserId.Should().BeEmpty();
        booking.StartTime.Should().Be(default);
        booking.EndTime.Should().Be(default);
        booking.Status.Should().Be("Active");
    }
}
