namespace IdentityApi.Infrastructure.Messaging.Contracts;
public sealed record SendEmailMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string To { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}
