using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TranslyAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    PromptInstruction = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    PromptVersion = table.Column<int>(type: "int", nullable: false),
                    IsListed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tones", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Tones_Code",
                table: "Tones",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tones_IsDefault",
                table: "Tones",
                column: "IsDefault",
                unique: true);

            migrationBuilder.InsertData(
                table: "Tones",
                columns: ["Id", "Code", "Name", "Description", "PromptInstruction", "PromptVersion", "IsListed", "IsDefault", "SortOrder"],
                values: new object[,]
                {
                    { 1, "formal", "Formal", "Polished and professional, for work and official messages.", "Use a formal, professional register.", 1, true, true, 1 },
                    { 2, "casual", "Casual", "Relaxed and conversational, for friends and everyday chats.", "Use a relaxed, conversational register.", 1, true, null, 2 },
                    { 3, "short", "Short", "As brief as possible while keeping the meaning.", "Translate as briefly as possible while preserving meaning.", 1, true, null, 3 },
                });

            migrationBuilder.Sql("DELETE FROM CachedTranslations;");

            migrationBuilder.DropColumn(
                name: "Tone",
                table: "CachedTranslations");

            migrationBuilder.AddColumn<int>(
                name: "ToneId",
                table: "CachedTranslations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PromptVersion",
                table: "CachedTranslations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ToneId",
                table: "TranslationUsages",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE TranslationUsages u
                JOIN Tones t ON t.Code = LOWER(u.Tone)
                SET u.ToneId = t.Id;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ToneId",
                table: "TranslationUsages",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Tone",
                table: "TranslationUsages");

            migrationBuilder.CreateIndex(
                name: "IX_TranslationUsages_ToneId",
                table: "TranslationUsages",
                column: "ToneId");

            migrationBuilder.CreateIndex(
                name: "IX_CachedTranslations_ToneId",
                table: "CachedTranslations",
                column: "ToneId");

            migrationBuilder.AddForeignKey(
                name: "FK_CachedTranslations_Tones_ToneId",
                table: "CachedTranslations",
                column: "ToneId",
                principalTable: "Tones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TranslationUsages_Tones_ToneId",
                table: "TranslationUsages",
                column: "ToneId",
                principalTable: "Tones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CachedTranslations_Tones_ToneId",
                table: "CachedTranslations");

            migrationBuilder.DropForeignKey(
                name: "FK_TranslationUsages_Tones_ToneId",
                table: "TranslationUsages");

            migrationBuilder.DropIndex(
                name: "IX_TranslationUsages_ToneId",
                table: "TranslationUsages");

            migrationBuilder.DropIndex(
                name: "IX_CachedTranslations_ToneId",
                table: "CachedTranslations");

            migrationBuilder.AddColumn<string>(
                name: "Tone",
                table: "TranslationUsages",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE TranslationUsages u
                JOIN Tones t ON t.Id = u.ToneId
                SET u.Tone = CONCAT(UPPER(LEFT(t.Code, 1)), SUBSTRING(t.Code, 2));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Tone",
                table: "TranslationUsages",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "ToneId",
                table: "TranslationUsages");

            migrationBuilder.Sql("DELETE FROM CachedTranslations;");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "CachedTranslations");

            migrationBuilder.DropColumn(
                name: "ToneId",
                table: "CachedTranslations");

            migrationBuilder.AddColumn<string>(
                name: "Tone",
                table: "CachedTranslations",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropTable(
                name: "Tones");
        }
    }
}
