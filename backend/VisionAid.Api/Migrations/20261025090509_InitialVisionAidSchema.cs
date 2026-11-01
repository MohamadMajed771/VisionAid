using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace VisionAid.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialVisionAidSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "USERS",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    PreferredLanguage = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERS", x => x.UserId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "STREAM_SESSIONS",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STREAM_SESSIONS", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_STREAM_SESSIONS_USERS_UserId",
                        column: x => x.UserId,
                        principalTable: "USERS",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FRAMES",
                columns: table => new
                {
                    FrameId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FRAMES", x => x.FrameId);
                    table.ForeignKey(
                        name: "FK_FRAMES_STREAM_SESSIONS_SessionId",
                        column: x => x.SessionId,
                        principalTable: "STREAM_SESSIONS",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QUESTIONS",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    AskedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    QuestionText = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
                    InputType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QUESTIONS", x => x.QuestionId);
                    table.ForeignKey(
                        name: "FK_QUESTIONS_STREAM_SESSIONS_SessionId",
                        column: x => x.SessionId,
                        principalTable: "STREAM_SESSIONS",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ANALYZED_FRAMES",
                columns: table => new
                {
                    AnalyzedFrameId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FrameId = table.Column<int>(type: "int", nullable: false),
                    AnalyzedImagePath = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true),
                    DepthMapPath = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true),
                    SceneDataJson = table.Column<string>(type: "longtext", nullable: true),
                    ModelVersion = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ANALYZED_FRAMES", x => x.AnalyzedFrameId);
                    table.ForeignKey(
                        name: "FK_ANALYZED_FRAMES_FRAMES_FrameId",
                        column: x => x.FrameId,
                        principalTable: "FRAMES",
                        principalColumn: "FrameId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ANSWERS",
                columns: table => new
                {
                    AnswerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    AnalyzedFrameId = table.Column<int>(type: "int", nullable: false),
                    AnswerText = table.Column<string>(type: "varchar(8000)", maxLength: 8000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ANSWERS", x => x.AnswerId);
                    table.ForeignKey(
                        name: "FK_ANSWERS_ANALYZED_FRAMES_AnalyzedFrameId",
                        column: x => x.AnalyzedFrameId,
                        principalTable: "ANALYZED_FRAMES",
                        principalColumn: "AnalyzedFrameId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ANSWERS_QUESTIONS_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QUESTIONS",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DETECTED_OBJECTS",
                columns: table => new
                {
                    DetectedObjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AnalyzedFrameId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Confidence = table.Column<float>(type: "float", nullable: false),
                    Depth = table.Column<float>(type: "float", nullable: true),
                    BoundingBoxX = table.Column<float>(type: "float", nullable: false),
                    BoundingBoxY = table.Column<float>(type: "float", nullable: false),
                    BoundingBoxWidth = table.Column<float>(type: "float", nullable: false),
                    BoundingBoxHeight = table.Column<float>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DETECTED_OBJECTS", x => x.DetectedObjectId);
                    table.ForeignKey(
                        name: "FK_DETECTED_OBJECTS_ANALYZED_FRAMES_AnalyzedFrameId",
                        column: x => x.AnalyzedFrameId,
                        principalTable: "ANALYZED_FRAMES",
                        principalColumn: "AnalyzedFrameId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OCR_RESULTS",
                columns: table => new
                {
                    OCRResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AnalyzedFrameId = table.Column<int>(type: "int", nullable: false),
                    ExtractedText = table.Column<string>(type: "varchar(8000)", maxLength: 8000, nullable: false),
                    Confidence = table.Column<float>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCR_RESULTS", x => x.OCRResultId);
                    table.ForeignKey(
                        name: "FK_OCR_RESULTS_ANALYZED_FRAMES_AnalyzedFrameId",
                        column: x => x.AnalyzedFrameId,
                        principalTable: "ANALYZED_FRAMES",
                        principalColumn: "AnalyzedFrameId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ANALYZED_FRAMES_FrameId_AnalyzedAt",
                table: "ANALYZED_FRAMES",
                columns: new[] { "FrameId", "AnalyzedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ANSWERS_AnalyzedFrameId",
                table: "ANSWERS",
                column: "AnalyzedFrameId");

            migrationBuilder.CreateIndex(
                name: "IX_ANSWERS_QuestionId",
                table: "ANSWERS",
                column: "QuestionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DETECTED_OBJECTS_AnalyzedFrameId",
                table: "DETECTED_OBJECTS",
                column: "AnalyzedFrameId");

            migrationBuilder.CreateIndex(
                name: "IX_FRAMES_SessionId_CapturedAt",
                table: "FRAMES",
                columns: new[] { "SessionId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OCR_RESULTS_AnalyzedFrameId",
                table: "OCR_RESULTS",
                column: "AnalyzedFrameId");

            migrationBuilder.CreateIndex(
                name: "IX_QUESTIONS_SessionId",
                table: "QUESTIONS",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_STREAM_SESSIONS_UserId",
                table: "STREAM_SESSIONS",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_USERS_Email",
                table: "USERS",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ANSWERS");

            migrationBuilder.DropTable(
                name: "DETECTED_OBJECTS");

            migrationBuilder.DropTable(
                name: "OCR_RESULTS");

            migrationBuilder.DropTable(
                name: "QUESTIONS");

            migrationBuilder.DropTable(
                name: "ANALYZED_FRAMES");

            migrationBuilder.DropTable(
                name: "FRAMES");

            migrationBuilder.DropTable(
                name: "STREAM_SESSIONS");

            migrationBuilder.DropTable(
                name: "USERS");
        }
    }
}
