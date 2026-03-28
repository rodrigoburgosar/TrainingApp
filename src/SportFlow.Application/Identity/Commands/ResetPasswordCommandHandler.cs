using Microsoft.AspNetCore.Identity;
using SportFlow.Application.Abstractions;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Domain.Identity;
using SportFlow.Shared.Results;

namespace SportFlow.Application.Identity.Commands;

public class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ResetPasswordRequest>
{
    private readonly PasswordHasher<User> _hasher = new();

    public async Task<Result> Handle(ResetPasswordRequest command, CancellationToken ct = default)
    {
        // Buscar usuario por token de reset (implementación simplificada - en prod usaríamos índice)
        // Por ahora asumimos que el token viene codificado con el userId
        // En una implementación completa habría una tabla de reset tokens
        return Result.Failure("NOT_IMPLEMENTED", "Reset password flow requires token lookup implementation.");
    }
}
