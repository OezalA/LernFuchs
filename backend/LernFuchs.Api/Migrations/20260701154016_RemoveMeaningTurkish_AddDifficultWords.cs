using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LernFuchs.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMeaningTurkish_AddDifficultWords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeaningTurkish",
                table: "VocabularyWords");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MeaningTurkish",
                table: "VocabularyWords",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }
    }
}
