using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuffetDiscovery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferingVideoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Offerings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Offerings");
        }
    }
}
