using FluentAssertions;
using Npgsql;
using NpgsqlTypes;

namespace StudioBooking.Api.IntegrationTests;

public class DoubleBookingSpikeTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public DoubleBookingSpikeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE EXTENSION IF NOT EXISTS btree_gist;

            CREATE TABLE IF NOT EXISTS test_bookings (
                id uuid,
                room_id int,
                booking_range tstzrange,
                status text
            );

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'no_overlapping_bookings'
                ) THEN
                    ALTER TABLE test_bookings
                    ADD CONSTRAINT no_overlapping_bookings
                    EXCLUDE USING gist (room_id WITH =, booking_range WITH &&)
                    WHERE (status = 'Active');
                END IF;
            END $$;
            """;

        await cmd.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task InsertBookingAsync(Guid id, int roomId, DateTimeOffset start, DateTimeOffset end, string status)
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO test_bookings (id, room_id, booking_range, status)
            VALUES (@id, @roomId, tstzrange(@start, @end, '[)'), @status);
            """;

        cmd.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = id });
        cmd.Parameters.Add(new NpgsqlParameter("roomId", NpgsqlDbType.Integer) { Value = roomId });
        cmd.Parameters.Add(new NpgsqlParameter("start", NpgsqlDbType.TimestampTz) { Value = start });
        cmd.Parameters.Add(new NpgsqlParameter("end", NpgsqlDbType.TimestampTz) { Value = end });
        cmd.Parameters.Add(new NpgsqlParameter("status", NpgsqlDbType.Text) { Value = status });

        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Overlap_TwoActiveBookingsInSameSlotForSameRoom_SecondThrows23P01()
    {
        // Arrange
        const int roomId = 101;
        var start1 = new DateTimeOffset(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);
        var end1 = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

        var start2 = new DateTimeOffset(2026, 10, 17, 11, 0, 0, TimeSpan.Zero);
        var end2 = new DateTimeOffset(2026, 10, 17, 13, 0, 0, TimeSpan.Zero);

        await InsertBookingAsync(Guid.NewGuid(), roomId, start1, end1, "Active");

        // Act
        var act = () => InsertBookingAsync(Guid.NewGuid(), roomId, start2, end2, "Active");

        // Assert
        var exception = await act.Should().ThrowAsync<PostgresException>();
        exception.Which.SqlState.Should().Be("23P01");
    }

    [Fact]
    public async Task BackToBack_BookingEndsAt14AndNextStartsAt14_BothSucceed()
    {
        // Arrange
        const int roomId = 102;
        var start1 = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        var end1 = new DateTimeOffset(2026, 10, 17, 14, 0, 0, TimeSpan.Zero);

        var start2 = new DateTimeOffset(2026, 10, 17, 14, 0, 0, TimeSpan.Zero);
        var end2 = new DateTimeOffset(2026, 10, 17, 16, 0, 0, TimeSpan.Zero);

        // Act & Assert
        var act1 = () => InsertBookingAsync(Guid.NewGuid(), roomId, start1, end1, "Active");
        await act1.Should().NotThrowAsync();

        var act2 = () => InsertBookingAsync(Guid.NewGuid(), roomId, start2, end2, "Active");
        await act2.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Cancelled_OneCancelledAndOneActiveInSameSlot_BothSucceed()
    {
        // Arrange
        const int roomId = 103;
        var start = new DateTimeOffset(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

        // Act & Assert
        var act1 = () => InsertBookingAsync(Guid.NewGuid(), roomId, start, end, "Cancelled");
        await act1.Should().NotThrowAsync();

        var act2 = () => InsertBookingAsync(Guid.NewGuid(), roomId, start, end, "Active");
        await act2.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Concurrency_TwoSimultaneousActiveBookingsForSameSlot_OnlyOneSucceedsAndOtherFailsWith23P01()
    {
        // Arrange
        const int roomId = 104;
        var start = new DateTimeOffset(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

        // Act
        var task1 = Task.Run(async () =>
        {
            try
            {
                await InsertBookingAsync(Guid.NewGuid(), roomId, start, end, "Active");
                return (Success: true, SqlState: (string?)null);
            }
            catch (PostgresException ex)
            {
                return (Success: false, SqlState: ex.SqlState);
            }
        });

        var task2 = Task.Run(async () =>
        {
            try
            {
                await InsertBookingAsync(Guid.NewGuid(), roomId, start, end, "Active");
                return (Success: true, SqlState: (string?)null);
            }
            catch (PostgresException ex)
            {
                return (Success: false, SqlState: ex.SqlState);
            }
        });

        var results = await Task.WhenAll(task1, task2);

        // Assert
        results.Count(r => r.Success).Should().Be(1);
        results.Count(r => !r.Success).Should().Be(1);
        results.Single(r => !r.Success).SqlState.Should().Be("23P01");
    }
}
