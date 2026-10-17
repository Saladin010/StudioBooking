using System.ComponentModel.DataAnnotations.Schema;

namespace StudioBooking.Domain.Entities;

public class Dispute
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid CustomerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? ResolutionNotes { get; set; }

    public Booking? Booking { get; set; }
    public User? Customer { get; set; }

    [NotMapped]
    public Guid UserId
    {
        get => CustomerId;
        set => CustomerId = value;
    }

    [NotMapped]
    public User? User
    {
        get => Customer;
        set => Customer = value;
    }

    public Dispute()
    {
    }

    public Dispute(Guid id, Guid bookingId, Guid customerId, string reason, string status = "Pending", string? resolutionNotes = null)
    {
        Id = id;
        BookingId = bookingId;
        CustomerId = customerId;
        Reason = reason;
        Status = status;
        ResolutionNotes = resolutionNotes;
    }
}
