using SportFlow.Application.Abstractions;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Domain.Tenants;
using SportFlow.Shared.Results;

namespace SportFlow.Application.Identity.Commands;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken ct = default);
}

public class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    IEmailService emailService,
    IUnitOfWork unitOfWork) : ICommandHandler<ForgotPasswordRequest>
{
    public async Task<Result> Handle(ForgotPasswordRequest command, CancellationToken ct = default)
    {
        // Siempre retorna éxito por seguridad (no revelar si el email existe)
        var tenant = await tenantRepository.GetBySlugAsync(command.TenantSlug, ct);
        if (tenant is null)
            return Result.Success();

        var tenantId = tenant.Id;
        var user = await userRepository.GetByEmailAndTenantAsync(command.Email.ToLowerInvariant(), tenantId, ct);
        if (user is null || !user.IsActive)
            return Result.Success();

        var resetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        user.SetPasswordResetToken(resetToken, DateTime.UtcNow.AddHours(2));

        await userRepository.UpdateAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Stub en v1 — en producción enviaría email real
        await emailService.SendPasswordResetEmailAsync(user.Email, resetToken, ct);

        return Result.Success();
    }
}
