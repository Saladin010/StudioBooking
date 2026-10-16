namespace StudioBooking.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public User()
    {
    }

    public User(Guid id, string email, string role)
    {
        Id = id;
        Email = email;
        Role = role;
    }
}
