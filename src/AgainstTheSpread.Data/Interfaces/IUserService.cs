using AgainstTheSpread.Data.Entities;

namespace AgainstTheSpread.Data.Interfaces;

/// <summary>
/// Service for managing user accounts in the Against The Spread application.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets a user by their Google OAuth subject identifier.
    /// </summary>
    /// <param name="googleSubjectId">The Google subject ID (sub claim).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<User?> GetByGoogleSubjectIdAsync(string googleSubjectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an existing user or creates a new one based on Google OAuth information.
    /// </summary>
    /// <param name="googleSubjectId">The Google subject ID (sub claim).</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing or newly created user.</returns>
    Task<User> GetOrCreateUserAsync(
        string googleSubjectId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last login timestamp for a user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the update was successful, false if user not found.</returns>
    Task<bool> UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their internal ID.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
