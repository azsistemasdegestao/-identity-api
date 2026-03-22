namespace IdentityApi.Domain.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetUrl, CancellationToken ct = default);
    Task SendWelcomeEmailAsync(string toEmail, string userName, CancellationToken ct = default);
}
