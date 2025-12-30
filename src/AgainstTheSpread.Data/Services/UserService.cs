using AgainstTheSpread.Data.Entities;
using AgainstTheSpread.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgainstTheSpread.Data.Services;

/// <summary>
/// Service for managing user accounts in the Against The Spread application.
/// </summary>
public class UserService : IUserService
{
    private readonly AtsDbContext _context;
    private readonly ILogger<UserService> _logger;

    /// <summary>
    /// Initializes a new instance of UserService.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public UserService(AtsDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<User?> GetByGoogleSubjectIdAsync(string googleSubjectId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.GoogleSubjectId == googleSubjectId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User> GetOrCreateUserAsync(
        string googleSubjectId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        // Try to get existing user
        var existingUser = await GetByGoogleSubjectIdAsync(googleSubjectId, cancellationToken);

        if (existingUser != null)
        {
            // Update last login time
            existingUser.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {Email} logged in successfully", existingUser.Email);
            return existingUser;
        }

        // Create new user
        var now = DateTime.UtcNow;
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            GoogleSubjectId = googleSubjectId,
            Email = email,
            DisplayName = string.IsNullOrEmpty(displayName) ? email : displayName,
            CreatedAt = now,
            LastLoginAt = now,
            IsActive = true
        };

        _context.Users.Add(newUser);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created new user {Email} with ID {UserId}", email, newUser.Id);
        }
        catch (DbUpdateException ex)
        {
            // Handle potential concurrent creation - try to get the user again
            _logger.LogWarning(ex, "Concurrent user creation detected for {GoogleSubjectId}", googleSubjectId);

            // Detach the new user we tried to add
            _context.Entry(newUser).State = EntityState.Detached;

            // Try to get the user that was created by another request
            var concurrentUser = await GetByGoogleSubjectIdAsync(googleSubjectId, cancellationToken);
            if (concurrentUser != null)
            {
                return concurrentUser;
            }

            // Re-throw if we still can't find the user
            throw;
        }

        return newUser;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Attempted to update last login for non-existent user {UserId}", userId);
            return false;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated last login for user {UserId}", userId);
        return true;
    }

    /// <inheritdoc/>
    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
    }
}
