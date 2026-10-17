using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudioBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResolutionNotesToDispute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResolutionNotes",
                table: "Disputes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResolutionNotes",
                table: "Disputes");
        }
    }
}
