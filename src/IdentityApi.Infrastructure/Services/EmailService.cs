using IdentityApi.Domain.Services;
using IdentityApi.Infrastructure.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace IdentityApi.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IPublishEndpoint publishEndpoint, ILogger<EmailService> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetUrl, CancellationToken ct = default)
    {
        var body = $"""
            <html>
            <body>
                <h2>Password Reset Request</h2>
                <p>Hello {userName},</p>
                <p>We received a request to reset your password. Click the link below to reset it:</p>
                <p><a href="{resetUrl}">Reset Password</a></p>
                <p>If you didn't request this, you can safely ignore this email.</p>
                <p>This link will expire in 24 hours.</p>
            </body>
            </html>
            """;

        var message = new SendEmailMessage
        {
            To = toEmail,
            Subject = "Password Reset Request",
            Body = body
        };

        await _publishEndpoint.Publish(message, ct);
        _logger.LogInformation("Password reset email queued for {Email}.", MaskEmail(toEmail));
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userName, CancellationToken ct = default)
    {
        var body = $"""
            <html>
            <body>
                <h2>Welcome to Identity API!</h2>
                <p>Hello {userName},</p>
                <p>Your account has been successfully created. You can now log in and start using the service.</p>
                <p>Thank you for joining us!</p>
            </body>
            </html>
            """;

        var message = new SendEmailMessage
        {
            To = toEmail,
            Subject = "Welcome to Identity API",
            Body = body
        };

        await _publishEndpoint.Publish(message, ct);
        _logger.LogInformation("Welcome email queued for {Email}.", MaskEmail(toEmail));
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return "***";
        return email[0] + "***" + email[atIndex..];
    }
}
