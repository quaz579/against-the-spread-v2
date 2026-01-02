namespace AgainstTheSpread.Core.Interfaces;

/// <summary>
/// Service for archiving Excel files to Azure Blob Storage.
/// Used for backup/audit purposes only - not for data retrieval.
/// </summary>
public interface IArchiveService
{
    /// <summary>
    /// Archives weekly lines Excel file to blob storage.
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="week">Week number</param>
    /// <param name="year">Year</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>URL of the archived blob</returns>
    Task<string> ArchiveWeeklyLinesAsync(
        Stream excelStream,
        int week,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives bowl lines Excel file to blob storage.
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="year">Year</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>URL of the archived blob</returns>
    Task<string> ArchiveBowlLinesAsync(
        Stream excelStream,
        int year,
        CancellationToken cancellationToken = default);
}
