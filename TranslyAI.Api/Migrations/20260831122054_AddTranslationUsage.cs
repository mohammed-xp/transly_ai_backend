using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TranslyAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTranslationUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TranslationUsages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SourceLanguage = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    TargetLanguage = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    Tone = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    CharacterCount = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranslationUsages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TranslationUsages_UserId",
                table: "TranslationUsages",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TranslationUsages");
        }
    }
}
