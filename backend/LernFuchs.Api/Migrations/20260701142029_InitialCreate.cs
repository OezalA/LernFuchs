using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LernFuchs.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReadingPassages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Difficulty = table.Column<string>(type: "TEXT", nullable: false),
                    Topic = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    WordCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingPassages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyWords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Word = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Article = table.Column<string>(type: "TEXT", nullable: false),
                    Plural = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    WordType = table.Column<string>(type: "TEXT", nullable: false),
                    MeaningTurkish = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DefinitionGerman = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ExampleSentence = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Synonyms = table.Column<string>(type: "TEXT", nullable: false),
                    Antonyms = table.Column<string>(type: "TEXT", nullable: false),
                    Difficulty = table.Column<string>(type: "TEXT", nullable: false),
                    Topic = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyWords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComprehensionQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReadingPassageId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionText = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    QuestionType = table.Column<string>(type: "TEXT", nullable: false),
                    Options = table.Column<string>(type: "TEXT", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    Explanation = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprehensionQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprehensionQuestions_ReadingPassages_ReadingPassageId",
                        column: x => x.ReadingPassageId,
                        principalTable: "ReadingPassages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VocabularyWordId = table.Column<int>(type: "INTEGER", nullable: false),
                    Box = table.Column<int>(type: "INTEGER", nullable: false),
                    TimesCorrect = table.Column<int>(type: "INTEGER", nullable: false),
                    TimesWrong = table.Column<int>(type: "INTEGER", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextReviewAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabularyProgress_VocabularyWords_VocabularyWordId",
                        column: x => x.VocabularyWordId,
                        principalTable: "VocabularyWords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComprehensionQuestions_ReadingPassageId",
                table: "ComprehensionQuestions",
                column: "ReadingPassageId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyProgress_VocabularyWordId",
                table: "VocabularyProgress",
                column: "VocabularyWordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyWords_Word",
                table: "VocabularyWords",
                column: "Word");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComprehensionQuestions");

            migrationBuilder.DropTable(
                name: "VocabularyProgress");

            migrationBuilder.DropTable(
                name: "ReadingPassages");

            migrationBuilder.DropTable(
                name: "VocabularyWords");
        }
    }
}
