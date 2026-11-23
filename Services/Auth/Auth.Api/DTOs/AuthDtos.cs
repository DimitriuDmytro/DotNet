using System.ComponentModel.DataAnnotations;

namespace Services.Auth.Api.DTOs;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    string? UserName = null,
    string? FirstName = null,
    string? LastName = null
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string UserId,
    string Email,
    IReadOnlyList<string> Roles
);

public record RefreshTokenRequest(
    string? RefreshToken = null
);

public record RevokeTokenRequest(
    string? RefreshToken = null
);

public record VerifyEmailRequest(
    [Required] string UserId,
    [Required] string Token
);

public record ResendVerificationRequest(
    [Required, EmailAddress] string Email
);

public record ForgotPasswordRequest(
    [Required, EmailAddress] string Email
);

public record ResetPasswordRequest(
    [Required, EmailAddress] string Email,
    [Required] string Token,
    [Required, MinLength(8)] string NewPassword
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword
);

public record UserDto(
    string Id,
    string? UserName,
    string? Email,
    bool EmailConfirmed,
    string? FirstName,
    string? LastName,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles
);

public record UpdateUserRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber
);

public record AddRoleRequest(
    [Required] string Role
);

public record RefreshTokenDto(
    int Id,
    string TokenHash,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsActive,
    bool IsRevoked,
    string? CreatedByIp
);
