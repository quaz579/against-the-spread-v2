using AgainstTheSpread.Data.Entities;
using System.Text.Json;

namespace AgainstTheSpread.Data.Seeds;

/// <summary>
/// Generates seed data for team aliases from the logo mapping JSON.
/// Groups teams by ESPN ID and picks a canonical name.
/// </summary>
public static class TeamAliasSeedData
{
    /// <summary>
    /// Generates team alias entities from the logo mapping JSON.
    /// </summary>
    /// <param name="logoMappingJsonPath">Path to team-logo-mapping.json</param>
    /// <returns>List of TeamAliasEntity records for seeding.</returns>
    public static List<TeamAliasEntity> GenerateFromLogoMapping(string logoMappingJsonPath)
    {
        var json = File.ReadAllText(logoMappingJsonPath);
        var mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? throw new InvalidOperationException("Failed to parse logo mapping JSON");

        return GenerateFromMapping(mapping);
    }

    /// <summary>
    /// Generates team alias entities from a team name to ESPN ID mapping.
    /// </summary>
    public static List<TeamAliasEntity> GenerateFromMapping(Dictionary<string, string> teamNameToEspnId)
    {
        var now = DateTime.UtcNow;
        var aliases = new List<TeamAliasEntity>();

        // Group by ESPN ID to find all names for the same team
        var groupedByEspnId = teamNameToEspnId
            .GroupBy(kvp => kvp.Value)
            .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).ToList());

        foreach (var (espnId, names) in groupedByEspnId)
        {
            // Pick the canonical name using heuristics
            var canonicalName = PickCanonicalName(names);

            // Create alias entries for all names (including canonical)
            foreach (var name in names)
            {
                aliases.Add(new TeamAliasEntity
                {
                    Alias = name,
                    CanonicalName = canonicalName,
                    CreatedAt = now
                });
            }
        }

        return aliases.OrderBy(a => a.CanonicalName).ThenBy(a => a.Alias).ToList();
    }

    /// <summary>
    /// Picks the canonical name from a list of aliases for the same team.
    /// Prefers longer names that look like full names (not abbreviations).
    /// </summary>
    private static string PickCanonicalName(List<string> names)
    {
        if (names.Count == 1)
        {
            return names[0];
        }

        // Manual overrides for well-known teams where the shorter name is official
        var knownCanonicals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Schools where the short form is the official name
            { "BYU", "BYU" },
            { "SMU", "SMU" },
            { "TCU", "TCU" },
            { "UCF", "UCF" },
            { "UCLA", "UCLA" },
            { "UNLV", "UNLV" },
            { "USC", "USC" },
            { "LSU", "LSU" },
            { "UAB", "UAB" },
            { "UTEP", "UTEP" },
            { "UTSA", "UTSA" },
        };

        // Check if any name in the list is a known canonical
        foreach (var name in names)
        {
            if (knownCanonicals.TryGetValue(name, out var canonical))
            {
                return canonical;
            }
        }

        // Filter out likely abbreviations (all caps, very short, or contain periods)
        var candidates = names
            .Where(n => !IsLikelyAbbreviation(n))
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = names; // Fall back to all names if all are abbreviations
        }

        // Prefer names without "State", "St", "St." variants - pick the fullest form
        // e.g., prefer "Florida State" over "Florida St" or "Florida St."
        var withState = candidates.Where(n => n.Contains(" State")).ToList();
        if (withState.Count > 0)
        {
            return withState.OrderByDescending(n => n.Length).First();
        }

        // Prefer the longest name that looks like a full name
        return candidates
            .OrderByDescending(n => n.Length)
            .ThenBy(n => n)
            .First();
    }

    /// <summary>
    /// Determines if a name is likely an abbreviation.
    /// </summary>
    private static bool IsLikelyAbbreviation(string name)
    {
        // Very short names are likely abbreviations
        if (name.Length <= 4)
        {
            return true;
        }

        // All caps suggests abbreviation (except for known full names)
        if (name.All(c => char.IsUpper(c) || !char.IsLetter(c)))
        {
            return true;
        }

        // Contains periods like "St." or "Mt."
        if (name.Contains('.'))
        {
            return true;
        }

        // Ends with " St" (short for State)
        if (name.EndsWith(" St", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the hardcoded seed data for teams.
    /// This is generated from team-logo-mapping.json and can be used directly in migrations.
    /// </summary>
    public static List<TeamAliasEntity> GetSeedData()
    {
        var now = DateTime.UtcNow;

        // This mapping is derived from team-logo-mapping.json grouped by ESPN ID
        // Format: CanonicalName -> [Alias1, Alias2, ...]
        var teamAliases = new Dictionary<string, string[]>
        {
            ["Abilene Christian"] = new[] { "Abilene Christian" },
            ["Air Force"] = new[] { "Air Force" },
            ["Akron"] = new[] { "Akron" },
            ["Alabama"] = new[] { "Alabama" },
            ["Albany"] = new[] { "Albany" },
            ["Appalachian State"] = new[] { "Appalachian State", "App State" },
            ["Arizona"] = new[] { "Arizona" },
            ["Arizona State"] = new[] { "Arizona State", "ASU" },
            ["Arkansas"] = new[] { "Arkansas" },
            ["Arkansas State"] = new[] { "Arkansas State", "Ark State" },
            ["Army"] = new[] { "Army" },
            ["Auburn"] = new[] { "Auburn" },
            ["Ball State"] = new[] { "Ball State" },
            ["Baylor"] = new[] { "Baylor" },
            ["Boise State"] = new[] { "Boise State", "Boise St", "Boise St." },
            ["Boston College"] = new[] { "Boston College", "BC" },
            ["Bowling Green"] = new[] { "Bowling Green", "BGSU" },
            ["Bryant"] = new[] { "Bryant" },
            ["Buffalo"] = new[] { "Buffalo" },
            ["BYU"] = new[] { "BYU", "Brigham Young" },
            ["California"] = new[] { "California", "Cal" },
            ["Central Arkansas"] = new[] { "Central Arkansas" },
            ["Central Michigan"] = new[] { "Central Michigan", "CMU" },
            ["Charlotte"] = new[] { "Charlotte" },
            ["Cincinnati"] = new[] { "Cincinnati" },
            ["Clemson"] = new[] { "Clemson" },
            ["Coastal Carolina"] = new[] { "Coastal Carolina" },
            ["Colorado"] = new[] { "Colorado" },
            ["Colorado State"] = new[] { "Colorado State", "Colorado St", "Colorado St." },
            ["Connecticut"] = new[] { "Connecticut", "UConn" },
            ["Delaware"] = new[] { "Delaware" },
            ["Delaware State"] = new[] { "Delaware State" },
            ["Duke"] = new[] { "Duke" },
            ["East Carolina"] = new[] { "East Carolina", "ECU" },
            ["Eastern Kentucky"] = new[] { "Eastern Kentucky" },
            ["Eastern Michigan"] = new[] { "Eastern Michigan", "EMU" },
            ["Elon"] = new[] { "Elon" },
            ["Florida"] = new[] { "Florida" },
            ["Florida Atlantic"] = new[] { "Florida Atlantic", "FAU" },
            ["Florida International"] = new[] { "Florida International", "FIU" },
            ["Florida State"] = new[] { "Florida State", "Florida St", "Florida St.", "FSU" },
            ["Fresno State"] = new[] { "Fresno State", "Fresno St", "Fresno St." },
            ["Georgia"] = new[] { "Georgia" },
            ["Georgia Southern"] = new[] { "Georgia Southern", "Ga Southern" },
            ["Georgia State"] = new[] { "Georgia State", "Ga State" },
            ["Georgia Tech"] = new[] { "Georgia Tech", "Ga Tech", "GT" },
            ["Hawaii"] = new[] { "Hawaii" },
            ["Houston"] = new[] { "Houston" },
            ["Idaho"] = new[] { "Idaho" },
            ["Illinois"] = new[] { "Illinois" },
            ["Indiana"] = new[] { "Indiana" },
            ["Iowa"] = new[] { "Iowa" },
            ["Iowa State"] = new[] { "Iowa State", "Iowa St", "Iowa St." },
            ["Jacksonville State"] = new[] { "Jacksonville State", "Jacksonville St", "Jacksonville St.", "Jax State" },
            ["James Madison"] = new[] { "James Madison", "JMU" },
            ["Kansas"] = new[] { "Kansas" },
            ["Kansas State"] = new[] { "Kansas State", "Kansas St", "Kansas St.", "K-State", "KSU" },
            ["Kennesaw State"] = new[] { "Kennesaw State" },
            ["Kent State"] = new[] { "Kent State", "Kent St", "Kent St." },
            ["Kentucky"] = new[] { "Kentucky" },
            ["Lamar"] = new[] { "Lamar" },
            ["Liberty"] = new[] { "Liberty" },
            ["Louisiana"] = new[] { "Louisiana", "ULL", "UL Lafayette" },
            ["Louisiana Tech"] = new[] { "Louisiana Tech", "La Tech" },
            ["Louisville"] = new[] { "Louisville" },
            ["LSU"] = new[] { "LSU", "Louisiana State" },
            ["Marshall"] = new[] { "Marshall" },
            ["Maryland"] = new[] { "Maryland" },
            ["Memphis"] = new[] { "Memphis" },
            ["Merrimack"] = new[] { "Merrimack" },
            ["Miami"] = new[] { "Miami", "Miami FL", "Miami (FL)", "The U" },
            ["Miami (OH)"] = new[] { "Miami (OH)", "Miami OH" },
            ["Michigan"] = new[] { "Michigan" },
            ["Michigan State"] = new[] { "Michigan State", "Michigan St", "Michigan St.", "MSU", "Mich State", "Mich St", "Mich St." },
            ["Middle Tennessee"] = new[] { "Middle Tennessee", "Middle Tennessee State", "MTSU", "MT" },
            ["Minnesota"] = new[] { "Minnesota" },
            ["Mississippi State"] = new[] { "Mississippi State", "Mississippi St", "Mississippi St.", "Miss State", "Miss St", "Miss St." },
            ["Missouri"] = new[] { "Missouri", "Mizzou" },
            ["Missouri State"] = new[] { "Missouri State" },
            ["Navy"] = new[] { "Navy" },
            ["Nebraska"] = new[] { "Nebraska" },
            ["Nevada"] = new[] { "Nevada" },
            ["New Mexico"] = new[] { "New Mexico" },
            ["New Mexico State"] = new[] { "New Mexico State", "New Mexico St", "New Mexico St.", "NMSU" },
            ["North Arizona"] = new[] { "North Arizona" },
            ["North Carolina"] = new[] { "North Carolina", "UNC" },
            ["NC State"] = new[] { "NC State", "N.C. State", "North Carolina State", "NCSU" },
            ["North Dakota"] = new[] { "North Dakota" },
            ["North Texas"] = new[] { "North Texas" },
            ["Northern Illinois"] = new[] { "Northern Illinois", "NIU" },
            ["Northwestern"] = new[] { "Northwestern" },
            ["Notre Dame"] = new[] { "Notre Dame" },
            ["Ohio"] = new[] { "Ohio" },
            ["Ohio State"] = new[] { "Ohio State", "Ohio St", "Ohio St.", "OSU" },
            ["Oklahoma"] = new[] { "Oklahoma" },
            ["Oklahoma State"] = new[] { "Oklahoma State", "Oklahoma St", "Oklahoma St.", "OK State", "OK St", "OK St." },
            ["Old Dominion"] = new[] { "Old Dominion", "ODU" },
            ["Ole Miss"] = new[] { "Ole Miss", "Mississippi" },
            ["Oregon"] = new[] { "Oregon" },
            ["Oregon State"] = new[] { "Oregon State", "Oregon St", "Oregon St." },
            ["Penn State"] = new[] { "Penn State", "Penn St", "Penn St.", "PSU" },
            ["Pittsburgh"] = new[] { "Pittsburgh", "Pitt" },
            ["Portland State"] = new[] { "Portland State" },
            ["Purdue"] = new[] { "Purdue" },
            ["Rice"] = new[] { "Rice" },
            ["Rutgers"] = new[] { "Rutgers" },
            ["Sam Houston"] = new[] { "Sam Houston", "Sam Houston State", "SHSU" },
            ["San Diego State"] = new[] { "San Diego State", "San Diego St", "San Diego St.", "SDSU" },
            ["San Jose State"] = new[] { "San Jose State", "San Jose St", "San Jose St.", "SJSU" },
            ["SE Louisiana"] = new[] { "SE Louisiana" },
            ["SF Austin"] = new[] { "SF Austin" },
            ["SMU"] = new[] { "SMU", "Southern Methodist" },
            ["South Alabama"] = new[] { "South Alabama" },
            ["South Carolina"] = new[] { "South Carolina" },
            ["South Florida"] = new[] { "South Florida", "USF" },
            ["Southern Miss"] = new[] { "Southern Miss", "Southern Mississippi", "So Miss", "So Mississippi" },
            ["St Francis PA"] = new[] { "St Francis PA" },
            ["Stanford"] = new[] { "Stanford" },
            ["Stony Brook"] = new[] { "Stony Brook" },
            ["Syracuse"] = new[] { "Syracuse", "Cuse" },
            ["TCU"] = new[] { "TCU", "Texas Christian" },
            ["Temple"] = new[] { "Temple" },
            ["Tennessee"] = new[] { "Tennessee", "Tenn" },
            ["Texas"] = new[] { "Texas" },
            ["Texas A&M"] = new[] { "Texas A&M", "Texas A&amp;M", "TAMU", "A&M" },
            ["Texas State"] = new[] { "Texas State", "Texas St", "Texas St.", "TXST" },
            ["Texas Tech"] = new[] { "Texas Tech" },
            ["Toledo"] = new[] { "Toledo" },
            ["Troy"] = new[] { "Troy" },
            ["Tulane"] = new[] { "Tulane" },
            ["Tulsa"] = new[] { "Tulsa" },
            ["UAB"] = new[] { "UAB" },
            ["UCF"] = new[] { "UCF" },
            ["UCLA"] = new[] { "UCLA" },
            ["UL Monroe"] = new[] { "UL Monroe", "ULM", "Louisiana Monroe" },
            ["UMass"] = new[] { "UMass", "Massachusetts" },
            ["UNLV"] = new[] { "UNLV" },
            ["USC"] = new[] { "USC", "Southern California", "Southern Cal" },
            ["UT Martin"] = new[] { "UT Martin" },
            ["UTEP"] = new[] { "UTEP", "Texas El Paso" },
            ["UTSA"] = new[] { "UTSA", "UT San Antonio" },
            ["Utah"] = new[] { "Utah" },
            ["Utah State"] = new[] { "Utah State", "Utah St", "Utah St." },
            ["Vanderbilt"] = new[] { "Vanderbilt", "Vandy" },
            ["Virginia"] = new[] { "Virginia", "UVA" },
            ["Virginia Tech"] = new[] { "Virginia Tech", "VA Tech", "VT", "VPI" },
            ["Wake Forest"] = new[] { "Wake Forest", "Wake" },
            ["Washington"] = new[] { "Washington" },
            ["Washington State"] = new[] { "Washington State", "Washington St", "Washington St.", "Wazzu", "WSU" },
            ["West Virginia"] = new[] { "West Virginia", "WVU" },
            ["Western Kentucky"] = new[] { "Western Kentucky", "WKU" },
            ["Western Michigan"] = new[] { "Western Michigan", "WMU" },
            ["Wisconsin"] = new[] { "Wisconsin", "Wisc" },
            ["Wyoming"] = new[] { "Wyoming" }
        };

        var result = new List<TeamAliasEntity>();
        var id = 1;

        foreach (var (canonical, aliases) in teamAliases.OrderBy(kvp => kvp.Key))
        {
            foreach (var alias in aliases)
            {
                result.Add(new TeamAliasEntity
                {
                    Id = id++,
                    Alias = alias,
                    CanonicalName = canonical,
                    CreatedAt = now
                });
            }
        }

        return result;
    }
}
