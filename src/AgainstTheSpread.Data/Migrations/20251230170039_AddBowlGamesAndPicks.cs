using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgainstTheSpread.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBowlGamesAndPicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BowlGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    GameNumber = table.Column<int>(type: "int", nullable: false),
                    BowlName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Favorite = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Underdog = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Line = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    GameDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FavoriteScore = table.Column<int>(type: "int", nullable: true),
                    UnderdogScore = table.Column<int>(type: "int", nullable: true),
                    SpreadWinner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsPush = table.Column<bool>(type: "bit", nullable: true),
                    OutrightWinner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResultEnteredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultEnteredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BowlGames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BowlPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BowlGameId = table.Column<int>(type: "int", nullable: false),
                    SpreadPick = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConfidencePoints = table.Column<int>(type: "int", nullable: false),
                    OutrightWinnerPick = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BowlPicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BowlPicks_BowlGames_BowlGameId",
                        column: x => x.BowlGameId,
                        principalTable: "BowlGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BowlPicks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BowlGames_Year",
                table: "BowlGames",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_BowlGames_Year_GameNumber",
                table: "BowlGames",
                columns: new[] { "Year", "GameNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BowlPicks_BowlGameId",
                table: "BowlPicks",
                column: "BowlGameId");

            migrationBuilder.CreateIndex(
                name: "IX_BowlPicks_UserId_BowlGameId",
                table: "BowlPicks",
                columns: new[] { "UserId", "BowlGameId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BowlPicks_UserId_Year",
                table: "BowlPicks",
                columns: new[] { "UserId", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_BowlPicks_UserId_Year_Confidence",
                table: "BowlPicks",
                columns: new[] { "UserId", "Year", "ConfidencePoints" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BowlPicks");

            migrationBuilder.DropTable(
                name: "BowlGames");
        }
    }
}
