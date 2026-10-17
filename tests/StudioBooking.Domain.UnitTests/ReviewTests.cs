using FluentAssertions;
using StudioBooking.Domain.Entities;

namespace StudioBooking.Domain.UnitTests;

public class ReviewTests
{
    [Fact]
    public void Constructor_WithValidArguments_InitializesPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const int rating = 5;
        const string comment = "Great studio with top-notch acoustics!";
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var review = new Review(id, roomId, userId, rating, comment, createdAt);

        // Assert
        review.Id.Should().Be(id);
        review.RoomId.Should().Be(roomId);
        review.UserId.Should().Be(userId);
        review.Rating.Should().Be(rating);
        review.Comment.Should().Be(comment);
        review.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void ParameterlessConstructor_InitializesEmptyDefaults()
    {
        // Act
        var review = new Review();

        // Assert
        review.Id.Should().BeEmpty();
        review.RoomId.Should().BeEmpty();
        review.UserId.Should().BeEmpty();
        review.Rating.Should().Be(0);
        review.Comment.Should().BeEmpty();
        review.CreatedAt.Should().Be(default);
    }
}
