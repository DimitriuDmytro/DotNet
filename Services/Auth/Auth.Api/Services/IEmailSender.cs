namespace Services.Auth.Api.Services;

public interface IEmailSender
{
    Task SendEmailConfirmationAsync(string email, string userId, string token, CancellationToken cancellationToken = default);
    Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default);
}
