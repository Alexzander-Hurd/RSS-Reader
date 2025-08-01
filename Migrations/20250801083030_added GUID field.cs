using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSS_Reader.Migrations
{
    /// <inheritdoc />
    public partial class addedGUIDfield : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "Entries",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Guid",
                table: "Entries");
        }
    }
}
