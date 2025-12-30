using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgainstTheSpread.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Week = table.Column<int>(type: "int", nullable: false),
                    Favorite = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Underdog = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Line = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    GameDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FavoriteScore = table.Column<int>(type: "int", nullable: true),
                    UnderdogScore = table.Column<int>(type: "int", nullable: true),
                    SpreadWinner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsPush = table.Column<bool>(type: "bit", nullable: true),
                    ResultEnteredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultEnteredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoogleSubjectId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Picks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    SelectedTeam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Week = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Picks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Picks_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Picks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_Year_Week",
                table: "Games",
                columns: new[] { "Year", "Week" });

            migrationBuilder.CreateIndex(
                name: "IX_Games_Year_Week_Favorite_Underdog",
                table: "Games",
                columns: new[] { "Year", "Week", "Favorite", "Underdog" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Picks_GameId",
                table: "Picks",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Picks_UserId_GameId",
                table: "Picks",
                columns: new[] { "UserId", "GameId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Picks_UserId_Year_Week",
                table: "Picks",
                columns: new[] { "UserId", "Year", "Week" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleSubjectId",
                table: "Users",
                column: "GoogleSubjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Picks");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
