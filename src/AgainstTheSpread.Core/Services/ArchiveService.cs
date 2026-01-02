using AgainstTheSpread.Core.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Core.Services;

/// <summary>
/// Service for archiving Excel files to Azure Blob Storage.
/// Used for backup/audit purposes only - data is read from database.
/// </summary>
public class ArchiveService : IArchiveService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<ArchiveService> _logger;
    private const string ContainerName = "gamefiles";
    private const string LinesFolder = "lines";
    private const string BowlLinesFolder = "bowl-lines";

    public ArchiveService(string connectionString, ILogger<ArchiveService> logger)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> ArchiveWeeklyLinesAsync(
        Stream excelStream,
        int week,
        int year,
        CancellationToken cancellationToken = default)
    {
        // Ensure container exists
        await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        // Archive Excel file
        var excelBlobName = $"{LinesFolder}/week-{week}-{year}.xlsx";
        var excelBlobClient = _containerClient.GetBlobClient(excelBlobName);

        excelStream.Position = 0;
        await excelBlobClient.UploadAsync(
            excelStream,
            overwrite: true,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Archived weekly lines Excel file: {BlobName}", excelBlobName);
        return excelBlobClient.Uri.ToString();
    }

    /// <inheritdoc/>
    public async Task<string> ArchiveBowlLinesAsync(
        Stream excelStream,
        int year,
        CancellationToken cancellationToken = default)
    {
        // Ensure container exists
        await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        // Archive Excel file
        var excelBlobName = $"{BowlLinesFolder}/bowls-{year}.xlsx";
        var excelBlobClient = _containerClient.GetBlobClient(excelBlobName);

        excelStream.Position = 0;
        await excelBlobClient.UploadAsync(
            excelStream,
            overwrite: true,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Archived bowl lines Excel file: {BlobName}", excelBlobName);
        return excelBlobClient.Uri.ToString();
    }
}
