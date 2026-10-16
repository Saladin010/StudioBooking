using FluentAssertions;
using StudioBooking.Domain.Entities;

namespace StudioBooking.Domain.UnitTests;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidArguments_InitializesPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        const string email = "artist@example.com";
        const string role = "Artist";

        // Act
        var user = new User(id, email, role);

        // Assert
        user.Id.Should().Be(id);
        user.Email.Should().Be(email);
        user.Role.Should().Be(role);
    }

    [Fact]
    public void ParameterlessConstructor_InitializesEmptyDefaults()
    {
        // Act
        var user = new User();

        // Assert
        user.Id.Should().BeEmpty();
        user.Email.Should().BeEmpty();
        user.Role.Should().BeEmpty();
    }
}
