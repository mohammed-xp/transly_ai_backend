using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranslyAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTranslationUsageQuotaIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TranslationUsages_UserId_CreatedAtUtc",
                table: "TranslationUsages",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TranslationUsages_UserId_CreatedAtUtc",
                table: "TranslationUsages");
        }
    }
}
