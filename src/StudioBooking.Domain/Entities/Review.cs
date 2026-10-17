namespace StudioBooking.Domain.Entities;

public class Review
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public Room? Room { get; set; }
    public User? User { get; set; }

    public Review()
    {
    }

    public Review(Guid id, Guid roomId, Guid userId, int rating, string comment, DateTimeOffset createdAt)
    {
        Id = id;
        RoomId = roomId;
        UserId = userId;
        Rating = rating;
        Comment = comment;
        CreatedAt = createdAt;
    }
}
