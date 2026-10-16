using FluentAssertions;
using StudioBooking.Domain.Entities;

namespace StudioBooking.Domain.UnitTests;

public class RoomTests
{
    [Fact]
    public void Constructor_WithValidArguments_InitializesPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        const string name = "Studio A";
        const int capacity = 8;
        const decimal hourlyRate = 150.50m;

        // Act
        var room = new Room(id, name, capacity, hourlyRate);

        // Assert
        room.Id.Should().Be(id);
        room.Name.Should().Be(name);
        room.Capacity.Should().Be(capacity);
        room.HourlyRate.Should().Be(hourlyRate);
    }

    [Fact]
    public void ParameterlessConstructor_InitializesEmptyDefaults()
    {
        // Act
        var room = new Room();

        // Assert
        room.Id.Should().BeEmpty();
        room.Name.Should().BeEmpty();
        room.Capacity.Should().Be(0);
        room.HourlyRate.Should().Be(0m);
    }
}
