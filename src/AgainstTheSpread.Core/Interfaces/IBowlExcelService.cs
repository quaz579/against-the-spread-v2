using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Interfaces;

/// <summary>
/// Service for parsing and generating Excel files for bowl games.
/// Separate from regular season to avoid breaking existing functionality.
/// </summary>
public interface IBowlExcelService
{
    /// <summary>
    /// Parses the bowl lines Excel file uploaded by admin
    /// </summary>
    Task<BowlLines> ParseBowlLinesAsync(Stream excelStream, int? year = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an Excel file with user's bowl picks in the required format
    /// </summary>
    Task<byte[]> GenerateBowlPicksExcelAsync(BowlUserPicks userPicks, CancellationToken cancellationToken = default);
}
