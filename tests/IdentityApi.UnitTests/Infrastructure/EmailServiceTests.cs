using FluentAssertions;
using IdentityApi.Infrastructure.Messaging.Contracts;
using IdentityApi.Infrastructure.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace IdentityApi.UnitTests.Infrastructure;

public sealed class EmailServiceTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<ILogger<EmailService>> _logger = new();
    private readonly EmailService _sut;

    public EmailServiceTests()
    {
        _sut = new EmailService(_publishEndpoint.Object, _logger.Object);
    }

    // ── SendWelcomeEmail ─────────────────────────────────────────────────────

    [Fact]
    public async Task SendWelcomeEmailAsync_PublishesMessage()
    {
        await _sut.SendWelcomeEmailAsync("alice@example.com", "Alice");

        _publishEndpoint.Verify(
            p => p.Publish(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_MessageContainsCorrectRecipient()
    {
        SendEmailMessage? captured = null;
        _publishEndpoint
            .Setup(p => p.Publish(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendEmailMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await _sut.SendWelcomeEmailAsync("alice@example.com", "Alice");

        captured.Should().NotBeNull();
        captured!.To.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_MessageSubjectIsNotEmpty()
    {
        SendEmailMessage? captured = null;
        _publishEndpoint
            .Setup(p => p.Publish(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendEmailMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await _sut.SendWelcomeEmailAsync("alice@example.com", "Alice");

        captured!.Subject.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_MessageBodyContainsUserName()
    {
        SendEmailMessage? captured = null;
        _publishEndpoint
            .Setup(p => p.Publish(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendEmailMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await _sut.SendWelcomeEmailAsync("alice@example.com", "Alice");

        captured!.Body.Should().Contain("Alice");
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_MessageHasId()
    {
        SendEmailMessage? captured = null;
        _publishEndpoint
            .Setup(p => p.Publish(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendEmailMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await _sut.SendWelcomeEmailAsync("alice@example.com", "Alice");

        captured!.MessageId.Should().NotBeEmpty();
    }

    // ── SendPasswordResetEmail ───────────────────────────────────────────────

    [Fact]
    public async Task SendPasswordResetEmailAsync_PublishesMessage()
    {
        await _sut.SendPasswordResetEmailAsync("alice@example.com", "Alice", "https://example.com/reset?token=abc");

        _publishEndpoint.Verify(
            p => p.Publish(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_MessageContainsResetUrl()
    {
        SendEmailMessage? captured = null;
        _publishEndpoint
            .Setup(p => p.Publish(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendEmailMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        const string resetUrl = "https://example.com/reset?token=abc";
        await _sut.SendPasswordResetEmailAsync("alice@example.com", "Alice", resetUrl);

        captured!.Body.Should().Contain(resetUrl);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_MessageContainsCorrectRecipient()
    {
        SendEmailMessage? captured = null;
        _publishEndpoint
            .Setup(p => p.Publish(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendEmailMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await _sut.SendPasswordResetEmailAsync("alice@example.com", "Alice", "https://example.com/reset");

        captured!.To.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_MessageBodyContainsUserName()
    {
        SendEmailMessage? captured = null;
        _publishEndpoint
            .Setup(p => p.Publish(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendEmailMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await _sut.SendPasswordResetEmailAsync("alice@example.com", "Alice", "https://example.com/reset");

        captured!.Body.Should().Contain("Alice");
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_RespectsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        CancellationToken capturedToken = default;
        _publishEndpoint
            .Setup(p => p.Publish(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendEmailMessage, CancellationToken>((_, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        await _sut.SendPasswordResetEmailAsync("alice@example.com", "Alice", "url", token);

        capturedToken.Should().Be(token);
    }
}
