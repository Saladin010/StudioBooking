namespace StudioBooking.Domain.Entities;

public class Room
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal HourlyRate { get; set; }

    public Room()
    {
    }

    public Room(Guid id, string name, int capacity, decimal hourlyRate)
    {
        Id = id;
        Name = name;
        Capacity = capacity;
        HourlyRate = hourlyRate;
    }
}
