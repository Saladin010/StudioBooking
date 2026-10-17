namespace StudioBooking.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Status { get; set; } = "Active";

    public Room? Room { get; set; }
    public User? User { get; set; }

    public Booking()
    {
    }

    public Booking(Guid id, Guid roomId, Guid userId, DateTimeOffset startTime, DateTimeOffset endTime, string status = "Active")
    {
        Id = id;
        RoomId = roomId;
        UserId = userId;
        StartTime = startTime;
        EndTime = endTime;
        Status = status;
    }
}
