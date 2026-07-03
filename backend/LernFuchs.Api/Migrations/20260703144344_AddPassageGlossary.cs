using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LernFuchs.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPassageGlossary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GlossaryJson",
                table: "ReadingPassages",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GlossaryJson",
                table: "ReadingPassages");
        }
    }
}
