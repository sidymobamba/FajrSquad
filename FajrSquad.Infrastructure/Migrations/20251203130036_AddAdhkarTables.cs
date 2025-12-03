using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FajrSquad.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdhkarTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adhkar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Arabic = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Transliteration = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Translation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Repetitions = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "it"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adhkar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAdhkarStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalCompleted = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LongestStreak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastCompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAdhkarStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAdhkarStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAdhkarFavorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdhkarId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAdhkarFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAdhkarFavorites_Adhkar_AdhkarId",
                        column: x => x.AdhkarId,
                        principalTable: "Adhkar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAdhkarFavorites_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAdhkarProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdhkarId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    CurrentCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAdhkarProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAdhkarProgress_Adhkar_AdhkarId",
                        column: x => x.AdhkarId,
                        principalTable: "Adhkar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAdhkarProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adhkar_Category_Language_IsActive",
                table: "Adhkar",
                columns: new[] { "Category", "Language", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Adhkar_Priority",
                table: "Adhkar",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_UserAdhkarFavorites_AdhkarId",
                table: "UserAdhkarFavorites",
                column: "AdhkarId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAdhkarFavorites_User_Adhkar",
                table: "UserAdhkarFavorites",
                columns: new[] { "UserId", "AdhkarId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAdhkarFavorites_UserId",
                table: "UserAdhkarFavorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAdhkarProgress_AdhkarId",
                table: "UserAdhkarProgress",
                column: "AdhkarId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAdhkarProgress_User_Adhkar_Date",
                table: "UserAdhkarProgress",
                columns: new[] { "UserId", "AdhkarId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAdhkarProgress_User_Date",
                table: "UserAdhkarProgress",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAdhkarStats_UserId",
                table: "UserAdhkarStats",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAdhkarFavorites");

            migrationBuilder.DropTable(
                name: "UserAdhkarProgress");

            migrationBuilder.DropTable(
                name: "UserAdhkarStats");

            migrationBuilder.DropTable(
                name: "Adhkar");
        }
    }
}
