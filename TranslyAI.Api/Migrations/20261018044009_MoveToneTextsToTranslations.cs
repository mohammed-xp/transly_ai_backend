using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranslyAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class MoveToneTextsToTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ToneTranslations",
                columns: table => new
                {
                    ToneId = table.Column<int>(type: "int", nullable: false),
                    Culture = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToneTranslations", x => new { x.ToneId, x.Culture });
                    table.ForeignKey(
                        name: "FK_ToneTranslations_Tones_ToneId",
                        column: x => x.ToneId,
                        principalTable: "Tones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.Sql(
                """
                INSERT INTO ToneTranslations (ToneId, Culture, Name, Description)
                SELECT Id, 'en', Name, Description FROM Tones;
                """);

            migrationBuilder.InsertData(
                table: "ToneTranslations",
                columns: ["ToneId", "Culture", "Name", "Description"],
                values: new object[,]
                {
                    { 1, "ar", "رسمي", "مصقول ومهني، للعمل والرسائل الرسمية." },
                    { 2, "ar", "ودّي", "بسيط وعفوي، للأصدقاء والمحادثات اليومية." },
                    { 3, "ar", "مختصر", "أقصر ما يمكن مع الحفاظ على المعنى." },
                });

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Tones");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Tones");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Tones",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Tones",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE Tones t
                JOIN ToneTranslations x ON x.ToneId = t.Id AND x.Culture = 'en'
                SET t.Name = x.Name, t.Description = x.Description;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tones",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Tones",
                type: "varchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.DropTable(
                name: "ToneTranslations");
        }
    }
}
