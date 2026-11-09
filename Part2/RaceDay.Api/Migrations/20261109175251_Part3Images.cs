using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class Part3Images : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePicture",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerImage",
                table: "Events",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BannerImage",
                table: "Events");
        }
    }
}
