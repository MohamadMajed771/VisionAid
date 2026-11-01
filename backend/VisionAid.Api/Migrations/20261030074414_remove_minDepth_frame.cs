using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisionAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class remove_minDepth_frame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinDepth",
                table: "FRAMES");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MinDepth",
                table: "FRAMES",
                type: "double",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
