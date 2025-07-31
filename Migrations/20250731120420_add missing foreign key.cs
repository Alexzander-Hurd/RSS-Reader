using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSS_Reader.Migrations
{
    /// <inheritdoc />
    public partial class addmissingforeignkey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entries_Feeds_FeedId",
                table: "Entries");

            migrationBuilder.AlterColumn<string>(
                name: "FeedId",
                table: "Entries",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Entries_Feeds_FeedId",
                table: "Entries",
                column: "FeedId",
                principalTable: "Feeds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entries_Feeds_FeedId",
                table: "Entries");

            migrationBuilder.AlterColumn<string>(
                name: "FeedId",
                table: "Entries",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_Entries_Feeds_FeedId",
                table: "Entries",
                column: "FeedId",
                principalTable: "Feeds",
                principalColumn: "Id");
        }
    }
}
