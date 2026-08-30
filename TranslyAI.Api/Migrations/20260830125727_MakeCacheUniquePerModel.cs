using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranslyAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeCacheUniquePerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CachedTranslations_CacheKey",
                table: "CachedTranslations");

            migrationBuilder.CreateIndex(
                name: "IX_CachedTranslations_CacheKey_Model",
                table: "CachedTranslations",
                columns: new[] { "CacheKey", "Model" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CachedTranslations_CacheKey_Model",
                table: "CachedTranslations");

            migrationBuilder.CreateIndex(
                name: "IX_CachedTranslations_CacheKey",
                table: "CachedTranslations",
                column: "CacheKey",
                unique: true);
        }
    }
}
