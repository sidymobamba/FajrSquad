using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FajrSquad.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class P32_ProfilePictureToText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ✅ Portiamo ProfilePicture a 'text' (illimitato)
            migrationBuilder.AlterColumn<string>(
                name: "ProfilePicture",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            // ✅ Aggiungiamo il timestamp preciso del check-in (UTC)
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInAtUtc",
                table: "FajrCheckIns",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ❌ Rimuove il campo CheckInAtUtc
            migrationBuilder.DropColumn(
                name: "CheckInAtUtc",
                table: "FajrCheckIns");

            // 🔒 Prima di ridurre a varchar(500), tronchiamo i valori lunghi per evitare 22001
            migrationBuilder.Sql(@"
                UPDATE ""Users""
                SET ""ProfilePicture"" = left(""ProfilePicture"", 500)
                WHERE length(""ProfilePicture"") > 500;
            ");

            // ⬅️ Torna a varchar(500) (se fai il rollback)
            migrationBuilder.AlterColumn<string>(
                name: "ProfilePicture",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
