using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisionAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class change_detected_Object : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BoundingBoxY",
                table: "DETECTED_OBJECTS",
                newName: "Y2");

            migrationBuilder.RenameColumn(
                name: "BoundingBoxX",
                table: "DETECTED_OBJECTS",
                newName: "Y1");

            migrationBuilder.RenameColumn(
                name: "BoundingBoxWidth",
                table: "DETECTED_OBJECTS",
                newName: "X2");

            migrationBuilder.RenameColumn(
                name: "BoundingBoxHeight",
                table: "DETECTED_OBJECTS",
                newName: "X1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Y2",
                table: "DETECTED_OBJECTS",
                newName: "BoundingBoxY");

            migrationBuilder.RenameColumn(
                name: "Y1",
                table: "DETECTED_OBJECTS",
                newName: "BoundingBoxX");

            migrationBuilder.RenameColumn(
                name: "X2",
                table: "DETECTED_OBJECTS",
                newName: "BoundingBoxWidth");

            migrationBuilder.RenameColumn(
                name: "X1",
                table: "DETECTED_OBJECTS",
                newName: "BoundingBoxHeight");
        }
    }
}
