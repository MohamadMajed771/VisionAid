using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisionAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class RequireFrameMinDepth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "ANSWERS");

            migrationBuilder.DropColumn(
                name: "DepthMapPath",
                table: "ANALYZED_FRAMES");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "ANALYZED_FRAMES");

            migrationBuilder.DropColumn(
                name: "SceneDataJson",
                table: "ANALYZED_FRAMES");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ANALYZED_FRAMES");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "STREAM_SESSIONS",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<double>(
                name: "MinDepth",
                table: "FRAMES",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<float>(
                name: "Depth",
                table: "DETECTED_OBJECTS",
                type: "float",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AnalyzedImagePath",
                table: "ANALYZED_FRAMES",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JsonFilePath",
                table: "ANALYZED_FRAMES",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinDepth",
                table: "FRAMES");

            migrationBuilder.DropColumn(
                name: "JsonFilePath",
                table: "ANALYZED_FRAMES");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "STREAM_SESSIONS",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Active");

            migrationBuilder.AlterColumn<float>(
                name: "Depth",
                table: "DETECTED_OBJECTS",
                type: "float",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "float");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ANSWERS",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "AnalyzedImagePath",
                table: "ANALYZED_FRAMES",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AddColumn<string>(
                name: "DepthMapPath",
                table: "ANALYZED_FRAMES",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "ANALYZED_FRAMES",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SceneDataJson",
                table: "ANALYZED_FRAMES",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ANALYZED_FRAMES",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }
    }
}
