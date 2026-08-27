using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TranslyAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTranslationCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CachedTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CacheKey = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    SourceLanguage = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    TargetLanguage = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    Tone = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    SourceText = table.Column<string>(type: "longtext", nullable: false),
                    TranslatedText = table.Column<string>(type: "longtext", nullable: false),
                    Model = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedTranslations", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CachedTranslations_CacheKey",
                table: "CachedTranslations",
                column: "CacheKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedTranslations");
        }
    }
}
