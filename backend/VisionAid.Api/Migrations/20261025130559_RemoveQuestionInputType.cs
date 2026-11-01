using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisionAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveQuestionInputType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InputType",
                table: "QUESTIONS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InputType",
                table: "QUESTIONS",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }
    }
}
