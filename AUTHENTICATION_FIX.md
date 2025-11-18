# Authentication Fix: Google OAuth Email Retrieval

## Issue Description

Users were experiencing 403 Forbidden errors when attempting to upload weekly lines through the admin interface. The error message was:

```
No email claim found for user 69a49c6de94e4493ba71051add2d5740
```

## Root Cause

When using Google OAuth authentication through Azure Static Web Apps (SWA), the user's email address can be stored in different locations depending on the OAuth provider configuration and the SWA authentication setup. 

The original code only checked two locations for the email:
1. The `email` claim in the Claims array
2. The `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress` claim in the Claims array

However, **Azure Static Web Apps with Google OAuth often stores the user's email in the `UserDetails` property** of the `ClientPrincipal` object, not in the Claims array. This was causing the authentication to fail even though the user was properly authenticated.

## Solution

The fix adds a third fallback location for email retrieval: the `UserDetails` property. The authentication flow now checks three locations in order:

1. **`email` claim type** - Standard OAuth claim
2. **`http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress` claim type** - Alternative OAuth claim format
3. **`UserDetails` property** (NEW) - Where Azure SWA with Google OAuth stores the email

### Code Changes

**File**: `src/AgainstTheSpread.Functions/UploadLinesFunction.cs`

**Before**:
```csharp
if (string.IsNullOrEmpty(userEmail))
{
    _logger.LogWarning("No email claim found for user {UserId}", principal.UserId);
    errorResponse = req.CreateResponse(HttpStatusCode.Forbidden);
    errorResponse.WriteStringAsync("Email not found in authentication").Wait();
    return false;
}
```

**After**:
```csharp
if (string.IsNullOrEmpty(userEmail))
{
    // Try UserDetails property - Google OAuth in SWA often stores email here
    userEmail = principal.UserDetails;
    _logger.LogInformation("Email retrieved from UserDetails for user {UserId}", principal.UserId);
}

if (string.IsNullOrEmpty(userEmail))
{
    _logger.LogWarning("No email found in claims or UserDetails for user {UserId}. Claims: {Claims}", 
        principal.UserId, 
        principal.Claims?.Select(c => $"{c.Type}={c.Value}").ToList() ?? new List<string>());
    errorResponse = req.CreateResponse(HttpStatusCode.Forbidden);
    errorResponse.WriteStringAsync("Email not found in authentication").Wait();
    return false;
}
```

### Additional Improvements

1. **Enhanced Logging**: When email is not found, the error message now includes all available claims to help debug authentication issues in the future.

2. **Informational Logging**: When email is successfully retrieved from `UserDetails`, it's logged for tracking which authentication method was used.

## Testing

- **Unit Tests**: Added `UploadLinesFunctionTests.cs` with tests documenting the fix
- **All Tests Passing**: 124/124 tests pass
- **Build Status**: Clean build with no errors
- **Security Scan**: CodeQL analysis found no security issues

## Impact

This fix ensures that admin users authenticated via Google OAuth through Azure Static Web Apps can successfully upload weekly lines, regardless of where the OAuth provider stores the email address.

## References

- Azure Static Web Apps Authentication Documentation: https://learn.microsoft.com/en-us/azure/static-web-apps/authentication-authorization
- ClientPrincipal structure: https://learn.microsoft.com/en-us/azure/static-web-apps/user-information?tabs=csharp

## Related Files

- `src/AgainstTheSpread.Functions/UploadLinesFunction.cs` - Main fix
- `src/AgainstTheSpread.Tests/Functions/UploadLinesFunctionTests.cs` - Test coverage
- `GOOGLE_AUTH_SETUP.md` - Authentication setup documentation
