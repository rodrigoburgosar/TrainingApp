using Microsoft.Extensions.Logging;
using SportFlow.Application.Identity.Commands;

namespace SportFlow.Infrastructure.Services;

/// <summary>
/// Stub email service for v1. Replace with real implementation (SendGrid, SMTP, etc.) in production.
/// </summary>
public class StubEmailService(ILogger<StubEmailService> logger) : IEmailService
{
    public Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken ct = default)
    {
        logger.LogInformation("Stub: password reset email for {Email} with token {Token}", email, resetToken);
        return Task.CompletedTask;
    }
}
