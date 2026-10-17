using FluentAssertions;
using StudioBooking.Domain.Entities;

namespace StudioBooking.Domain.UnitTests;

public class DisputeTests
{
    [Fact]
    public void Constructor_WithValidArguments_InitializesPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        const string reason = "Room was double booked";
        const string status = "Pending";

        // Act
        var dispute = new Dispute(id, bookingId, customerId, reason, status);

        // Assert
        dispute.Id.Should().Be(id);
        dispute.BookingId.Should().Be(bookingId);
        dispute.CustomerId.Should().Be(customerId);
        dispute.UserId.Should().Be(customerId);
        dispute.Reason.Should().Be(reason);
        dispute.Status.Should().Be(status);
    }

    [Fact]
    public void ParameterlessConstructor_InitializesEmptyDefaults()
    {
        // Act
        var dispute = new Dispute();

        // Assert
        dispute.Id.Should().BeEmpty();
        dispute.BookingId.Should().BeEmpty();
        dispute.CustomerId.Should().BeEmpty();
        dispute.UserId.Should().BeEmpty();
        dispute.Reason.Should().BeEmpty();
        dispute.Status.Should().Be("Pending");
    }
}
