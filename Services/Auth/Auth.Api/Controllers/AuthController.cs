using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Services.Auth.Api.DTOs;
using Services.Auth.Api.Models;
using Services.Auth.Api.Services;

namespace Services.Auth.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        IEmailSender emailSender,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "User with this email already exists." });
        }

        var userName = !string.IsNullOrWhiteSpace(request.UserName) ? request.UserName : request.Email;
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await _userManager.AddToRoleAsync(user, "User");

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _emailSender.SendEmailConfirmationAsync(user.Email, user.Id, token, cancellationToken);

        _logger.LogInformation("User {UserId} registered successfully with email {Email}", user.Id, user.Email);

        return Ok(new
        {
            message = "User registered successfully. Please verify your email.",
            userId = user.Id,
            email = user.Email
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            _logger.LogWarning("User {UserId} is locked out due to multiple failed login attempts", user.Id);
            return StatusCode(StatusCodes.Status423Locked, new { message = "Account is locked out. Please try again later." });
        }

        if (!signInResult.Succeeded)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("Login rejected for user {UserId}: Email {Email} is not confirmed", user.Id, user.Email);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Email is not confirmed. Please confirm your email before logging in." });
        }

        var ip = GetClientIp();
        var (accessToken, refreshToken) = await _jwtTokenService.GenerateTokensAsync(user, ip, cancellationToken);
        _jwtTokenService.SetRefreshTokenCookie(Response, refreshToken);

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AuthResponse(
            AccessToken: accessToken,
            TokenType: "Bearer",
            ExpiresIn: 900,
            UserId: user.Id,
            Email: user.Email ?? string.Empty,
            Roles: roles.ToList()
        ));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        var token = Request.Cookies[RefreshTokenCookieName] ?? request?.RefreshToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { message = "Refresh token is required (cookie or body)." });
        }

        try
        {
            var ip = GetClientIp();
            var (accessToken, newRefreshToken) = await _jwtTokenService.RotateRefreshTokenAsync(token, ip, cancellationToken);
            _jwtTokenService.SetRefreshTokenCookie(Response, newRefreshToken);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(accessToken);
            var userId = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var email = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email || c.Type == ClaimTypes.Email)?.Value ?? string.Empty;
            var roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").Select(c => c.Value).Distinct().ToList();

            return Ok(new AuthResponse(
                AccessToken: accessToken,
                TokenType: "Bearer",
                ExpiresIn: 900,
                UserId: userId,
                Email: email,
                Roles: roles
            ));
        }
        catch (SecurityTokenException ex)
        {
            _jwtTokenService.DeleteRefreshTokenCookie(Response);
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest? request, CancellationToken cancellationToken)
    {
        var token = Request.Cookies[RefreshTokenCookieName] ?? request?.RefreshToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { message = "Token is required." });
        }

        var ip = GetClientIp();
        var result = await _jwtTokenService.RevokeRefreshTokenAsync(token, ip, cancellationToken);
        if (!result)
        {
            return NotFound(new { message = "Token not found or already revoked." });
        }

        return Ok(new { message = "Token revoked successfully." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var token = Request.Cookies[RefreshTokenCookieName];
        if (!string.IsNullOrWhiteSpace(token))
        {
            var ip = GetClientIp();
            await _jwtTokenService.RevokeRefreshTokenAsync(token, ip, cancellationToken);
        }

        _jwtTokenService.DeleteRefreshTokenCookie(Response);
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return BadRequest(new { message = "Invalid user." });
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("Email confirmed for user {UserId}", user.Id);
        return Ok(new { message = "Email verified successfully. You can now login." });
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null && !user.EmailConfirmed)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _emailSender.SendEmailConfirmationAsync(user.Email!, user.Id, token, cancellationToken);
        }

        return Ok(new { message = "If the account exists and is unverified, a verification email has been sent." });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null && user.EmailConfirmed)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _emailSender.SendPasswordResetAsync(user.Email!, token, cancellationToken);
        }

        return Ok(new { message = "If the email is registered, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new { message = "Invalid password reset request." });
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        var ip = GetClientIp();
        await _jwtTokenService.RevokeAllUserTokensAsync(user.Id, ip, cancellationToken);
        _logger.LogInformation("Password reset successful and all refresh tokens revoked for user {UserId}", user.Id);

        return Ok(new { message = "Password has been reset successfully. Please login with your new password." });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("User {UserId} changed password successfully", user.Id);
        return Ok(new { message = "Password changed successfully." });
    }

    private string? GetClientIp()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
