using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgainstTheSpread.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CanonicalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamAliases", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamAliases_Alias",
                table: "TeamAliases",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamAliases_CanonicalName",
                table: "TeamAliases",
                column: "CanonicalName");

            // Seed team aliases data
            SeedTeamAliases(migrationBuilder);
        }

        private void SeedTeamAliases(MigrationBuilder migrationBuilder)
        {
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Team aliases grouped by canonical name
            var teamAliases = new (string Canonical, string[] Aliases)[]
            {
                ("Abilene Christian", new[] { "Abilene Christian" }),
                ("Air Force", new[] { "Air Force" }),
                ("Akron", new[] { "Akron" }),
                ("Alabama", new[] { "Alabama" }),
                ("Albany", new[] { "Albany" }),
                ("Appalachian State", new[] { "Appalachian State", "App State" }),
                ("Arizona", new[] { "Arizona" }),
                ("Arizona State", new[] { "Arizona State", "ASU" }),
                ("Arkansas", new[] { "Arkansas" }),
                ("Arkansas State", new[] { "Arkansas State", "Ark State" }),
                ("Army", new[] { "Army" }),
                ("Auburn", new[] { "Auburn" }),
                ("Ball State", new[] { "Ball State" }),
                ("Baylor", new[] { "Baylor" }),
                ("Boise State", new[] { "Boise State", "Boise St", "Boise St." }),
                ("Boston College", new[] { "Boston College", "BC" }),
                ("Bowling Green", new[] { "Bowling Green", "BGSU" }),
                ("Bryant", new[] { "Bryant" }),
                ("Buffalo", new[] { "Buffalo" }),
                ("BYU", new[] { "BYU", "Brigham Young" }),
                ("California", new[] { "California", "Cal" }),
                ("Central Arkansas", new[] { "Central Arkansas" }),
                ("Central Michigan", new[] { "Central Michigan", "CMU" }),
                ("Charlotte", new[] { "Charlotte" }),
                ("Cincinnati", new[] { "Cincinnati" }),
                ("Clemson", new[] { "Clemson" }),
                ("Coastal Carolina", new[] { "Coastal Carolina" }),
                ("Colorado", new[] { "Colorado" }),
                ("Colorado State", new[] { "Colorado State", "Colorado St", "Colorado St." }),
                ("Connecticut", new[] { "Connecticut", "UConn" }),
                ("Delaware", new[] { "Delaware" }),
                ("Delaware State", new[] { "Delaware State" }),
                ("Duke", new[] { "Duke" }),
                ("East Carolina", new[] { "East Carolina", "ECU" }),
                ("Eastern Kentucky", new[] { "Eastern Kentucky" }),
                ("Eastern Michigan", new[] { "Eastern Michigan", "EMU" }),
                ("Elon", new[] { "Elon" }),
                ("Florida", new[] { "Florida" }),
                ("Florida Atlantic", new[] { "Florida Atlantic", "FAU" }),
                ("Florida International", new[] { "Florida International", "FIU" }),
                ("Florida State", new[] { "Florida State", "Florida St", "Florida St.", "FSU" }),
                ("Fresno State", new[] { "Fresno State", "Fresno St", "Fresno St." }),
                ("Georgia", new[] { "Georgia" }),
                ("Georgia Southern", new[] { "Georgia Southern", "Ga Southern" }),
                ("Georgia State", new[] { "Georgia State", "Ga State" }),
                ("Georgia Tech", new[] { "Georgia Tech", "Ga Tech", "GT" }),
                ("Hawaii", new[] { "Hawaii" }),
                ("Houston", new[] { "Houston" }),
                ("Idaho", new[] { "Idaho" }),
                ("Illinois", new[] { "Illinois" }),
                ("Indiana", new[] { "Indiana" }),
                ("Iowa", new[] { "Iowa" }),
                ("Iowa State", new[] { "Iowa State", "Iowa St", "Iowa St." }),
                ("Jacksonville State", new[] { "Jacksonville State", "Jacksonville St", "Jacksonville St.", "Jax State" }),
                ("James Madison", new[] { "James Madison", "JMU" }),
                ("Kansas", new[] { "Kansas" }),
                ("Kansas State", new[] { "Kansas State", "Kansas St", "Kansas St.", "K-State", "KSU" }),
                ("Kennesaw State", new[] { "Kennesaw State" }),
                ("Kent State", new[] { "Kent State", "Kent St", "Kent St." }),
                ("Kentucky", new[] { "Kentucky" }),
                ("Lamar", new[] { "Lamar" }),
                ("Liberty", new[] { "Liberty" }),
                ("Louisiana", new[] { "Louisiana", "ULL", "UL Lafayette" }),
                ("Louisiana Tech", new[] { "Louisiana Tech", "La Tech" }),
                ("Louisville", new[] { "Louisville" }),
                ("LSU", new[] { "LSU", "Louisiana State" }),
                ("Marshall", new[] { "Marshall" }),
                ("Maryland", new[] { "Maryland" }),
                ("Memphis", new[] { "Memphis" }),
                ("Merrimack", new[] { "Merrimack" }),
                ("Miami", new[] { "Miami", "Miami FL", "Miami (FL)", "The U" }),
                ("Miami (OH)", new[] { "Miami (OH)", "Miami OH" }),
                ("Michigan", new[] { "Michigan" }),
                ("Michigan State", new[] { "Michigan State", "Michigan St", "Michigan St.", "MSU", "Mich State", "Mich St", "Mich St." }),
                ("Middle Tennessee", new[] { "Middle Tennessee", "Middle Tennessee State", "MTSU", "MT" }),
                ("Minnesota", new[] { "Minnesota" }),
                ("Mississippi State", new[] { "Mississippi State", "Mississippi St", "Mississippi St.", "Miss State", "Miss St", "Miss St." }),
                ("Missouri", new[] { "Missouri", "Mizzou" }),
                ("Missouri State", new[] { "Missouri State" }),
                ("Navy", new[] { "Navy" }),
                ("Nebraska", new[] { "Nebraska" }),
                ("Nevada", new[] { "Nevada" }),
                ("New Mexico", new[] { "New Mexico" }),
                ("New Mexico State", new[] { "New Mexico State", "New Mexico St", "New Mexico St.", "NMSU" }),
                ("North Arizona", new[] { "North Arizona" }),
                ("North Carolina", new[] { "North Carolina", "UNC" }),
                ("NC State", new[] { "NC State", "N.C. State", "North Carolina State", "NCSU" }),
                ("North Dakota", new[] { "North Dakota" }),
                ("North Texas", new[] { "North Texas" }),
                ("Northern Illinois", new[] { "Northern Illinois", "NIU" }),
                ("Northwestern", new[] { "Northwestern" }),
                ("Notre Dame", new[] { "Notre Dame" }),
                ("Ohio", new[] { "Ohio" }),
                ("Ohio State", new[] { "Ohio State", "Ohio St", "Ohio St.", "OSU" }),
                ("Oklahoma", new[] { "Oklahoma" }),
                ("Oklahoma State", new[] { "Oklahoma State", "Oklahoma St", "Oklahoma St.", "OK State", "OK St", "OK St." }),
                ("Old Dominion", new[] { "Old Dominion", "ODU" }),
                ("Ole Miss", new[] { "Ole Miss", "Mississippi" }),
                ("Oregon", new[] { "Oregon" }),
                ("Oregon State", new[] { "Oregon State", "Oregon St", "Oregon St." }),
                ("Penn State", new[] { "Penn State", "Penn St", "Penn St.", "PSU" }),
                ("Pittsburgh", new[] { "Pittsburgh", "Pitt" }),
                ("Portland State", new[] { "Portland State" }),
                ("Purdue", new[] { "Purdue" }),
                ("Rice", new[] { "Rice" }),
                ("Rutgers", new[] { "Rutgers" }),
                ("Sam Houston", new[] { "Sam Houston", "Sam Houston State", "SHSU" }),
                ("San Diego State", new[] { "San Diego State", "San Diego St", "San Diego St.", "SDSU" }),
                ("San Jose State", new[] { "San Jose State", "San Jose St", "San Jose St.", "SJSU" }),
                ("SE Louisiana", new[] { "SE Louisiana" }),
                ("SF Austin", new[] { "SF Austin" }),
                ("SMU", new[] { "SMU", "Southern Methodist" }),
                ("South Alabama", new[] { "South Alabama" }),
                ("South Carolina", new[] { "South Carolina" }),
                ("South Florida", new[] { "South Florida", "USF" }),
                ("Southern Miss", new[] { "Southern Miss", "Southern Mississippi", "So Miss", "So Mississippi" }),
                ("St Francis PA", new[] { "St Francis PA" }),
                ("Stanford", new[] { "Stanford" }),
                ("Stony Brook", new[] { "Stony Brook" }),
                ("Syracuse", new[] { "Syracuse", "Cuse" }),
                ("TCU", new[] { "TCU", "Texas Christian" }),
                ("Temple", new[] { "Temple" }),
                ("Tennessee", new[] { "Tennessee", "Tenn" }),
                ("Texas", new[] { "Texas" }),
                ("Texas A&M", new[] { "Texas A&M", "Texas A&amp;M", "TAMU", "A&M" }),
                ("Texas State", new[] { "Texas State", "Texas St", "Texas St.", "TXST" }),
                ("Texas Tech", new[] { "Texas Tech" }),
                ("Toledo", new[] { "Toledo" }),
                ("Troy", new[] { "Troy" }),
                ("Tulane", new[] { "Tulane" }),
                ("Tulsa", new[] { "Tulsa" }),
                ("UAB", new[] { "UAB" }),
                ("UCF", new[] { "UCF" }),
                ("UCLA", new[] { "UCLA" }),
                ("UL Monroe", new[] { "UL Monroe", "ULM", "Louisiana Monroe" }),
                ("UMass", new[] { "UMass", "Massachusetts" }),
                ("UNLV", new[] { "UNLV" }),
                ("USC", new[] { "USC", "Southern California", "Southern Cal" }),
                ("UT Martin", new[] { "UT Martin" }),
                ("UTEP", new[] { "UTEP", "Texas El Paso" }),
                ("UTSA", new[] { "UTSA", "UT San Antonio" }),
                ("Utah", new[] { "Utah" }),
                ("Utah State", new[] { "Utah State", "Utah St", "Utah St." }),
                ("Vanderbilt", new[] { "Vanderbilt", "Vandy" }),
                ("Virginia", new[] { "Virginia", "UVA" }),
                ("Virginia Tech", new[] { "Virginia Tech", "VA Tech", "VT", "VPI" }),
                ("Wake Forest", new[] { "Wake Forest", "Wake" }),
                ("Washington", new[] { "Washington" }),
                ("Washington State", new[] { "Washington State", "Washington St", "Washington St.", "Wazzu", "WSU" }),
                ("West Virginia", new[] { "West Virginia", "WVU" }),
                ("Western Kentucky", new[] { "Western Kentucky", "WKU" }),
                ("Western Michigan", new[] { "Western Michigan", "WMU" }),
                ("Wisconsin", new[] { "Wisconsin", "Wisc" }),
                ("Wyoming", new[] { "Wyoming" })
            };

            foreach (var (canonical, aliases) in teamAliases)
            {
                foreach (var alias in aliases)
                {
                    migrationBuilder.InsertData(
                        table: "TeamAliases",
                        columns: new[] { "Alias", "CanonicalName", "CreatedAt" },
                        values: new object[] { alias, canonical, now });
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamAliases");
        }
    }
}
