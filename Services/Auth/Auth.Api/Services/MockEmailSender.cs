using Microsoft.Extensions.Logging;

namespace Services.Auth.Api.Services;

public class MockEmailSender : IEmailSender
{
    private readonly ILogger<MockEmailSender> _logger;

    public MockEmailSender(ILogger<MockEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailConfirmationAsync(string email, string userId, string token, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockEmailSender: Email confirmation sent to {Email}. UserId: {UserId}, Token: {Token}", email, userId, token);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockEmailSender: Password reset sent to {Email}. Token: {Token}", email, token);
        return Task.CompletedTask;
    }
}
