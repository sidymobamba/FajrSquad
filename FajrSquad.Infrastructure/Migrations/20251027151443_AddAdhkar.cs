using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FajrSquad.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdhkar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adhkar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Categories = table.Column<string[]>(type: "text[]", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Repetitions = table.Column<int>(type: "integer", nullable: false),
                    SourceBook = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SourceRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    License = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Visible = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adhkar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "adhkar_set",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TitleIt = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Ord = table.Column<int>(type: "integer", nullable: false),
                    EveningStart = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EveningEnd = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adhkar_set", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_adhkar_bookmark",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdhkarId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_adhkar_bookmark", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_adhkar_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    TzId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MorningCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    MorningCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MorningSetId = table.Column<Guid>(type: "uuid", nullable: true),
                    EveningCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    EveningCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EveningSetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Counts = table.Column<Dictionary<string, int>>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_adhkar_progress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "adhkar_text",
                columns: table => new
                {
                    AdhkarId = table.Column<Guid>(type: "uuid", nullable: false),
                    Lang = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TextAr = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Transliteration = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Translation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adhkar_text", x => new { x.AdhkarId, x.Lang });
                    table.ForeignKey(
                        name: "FK_adhkar_text_adhkar_AdhkarId",
                        column: x => x.AdhkarId,
                        principalTable: "adhkar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "adhkar_set_item",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdhkarId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ord = table.Column<int>(type: "integer", nullable: false),
                    Repetitions = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adhkar_set_item", x => x.Id);
                    table.ForeignKey(
                        name: "FK_adhkar_set_item_adhkar_AdhkarId",
                        column: x => x.AdhkarId,
                        principalTable: "adhkar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_adhkar_set_item_adhkar_set_SetId",
                        column: x => x.SetId,
                        principalTable: "adhkar_set",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adhkar_Categories",
                table: "adhkar",
                column: "Categories")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_adhkar_Code",
                table: "adhkar",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adhkar_set_Code",
                table: "adhkar_set",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_adhkar_set_item_AdhkarId",
                table: "adhkar_set_item",
                column: "AdhkarId");

            migrationBuilder.CreateIndex(
                name: "IX_adhkar_set_item_SetId",
                table: "adhkar_set_item",
                column: "SetId");

            migrationBuilder.CreateIndex(
                name: "IX_user_adhkar_bookmark_UserId_AdhkarId",
                table: "user_adhkar_bookmark",
                columns: new[] { "UserId", "AdhkarId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_adhkar_progress_UserId_DateUtc",
                table: "user_adhkar_progress",
                columns: new[] { "UserId", "DateUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adhkar_set_item");

            migrationBuilder.DropTable(
                name: "adhkar_text");

            migrationBuilder.DropTable(
                name: "user_adhkar_bookmark");

            migrationBuilder.DropTable(
                name: "user_adhkar_progress");

            migrationBuilder.DropTable(
                name: "adhkar_set");

            migrationBuilder.DropTable(
                name: "adhkar");
        }
    }
}
