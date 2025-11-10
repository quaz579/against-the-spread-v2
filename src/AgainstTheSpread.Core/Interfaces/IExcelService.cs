using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Interfaces;

/// <summary>
/// Service for parsing and generating Excel files
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// Parses an Excel file containing weekly betting lines
    /// </summary>
    /// <param name="excelStream">Stream containing the Excel file data</param>
    /// <param name="week">Week number (optional, will be extracted from file if not provided)</param>
    /// <param name="year">Year (optional, will default to current year if not provided)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WeeklyLines object with parsed game data</returns>
    Task<WeeklyLines> ParseWeeklyLinesAsync(Stream excelStream, int? week = null, int? year = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an Excel file with user's picks in the expected format
    /// </summary>
    /// <param name="userPicks">User's picks to include in the Excel file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Byte array containing the generated Excel file</returns>
    Task<byte[]> GeneratePicksExcelAsync(UserPicks userPicks, CancellationToken cancellationToken = default);
}
