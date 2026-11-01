using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisionAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlignVisionAidSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "Confidence",
                table: "OCR_RESULTS",
                type: "float",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "float",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "Confidence",
                table: "OCR_RESULTS",
                type: "float",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "float");
        }
    }
}
