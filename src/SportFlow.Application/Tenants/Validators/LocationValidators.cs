using FluentValidation;
using SportFlow.Application.Tenants.DTOs;

namespace SportFlow.Application.Tenants.Validators;

public class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Timezone is required.")
            .Must(BeValidIanaTimezone).WithMessage("Timezone must be a valid IANA timezone identifier.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThan(0).When(x => x.MaxCapacity.HasValue)
            .WithMessage("MaxCapacity must be greater than 0.");
    }

    private static bool BeValidIanaTimezone(string timezone)
        => TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out _);
}

public class UpdateLocationRequestValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(x => x.Timezone)
            .Must(BeValidIanaTimezone).When(x => x.Timezone is not null)
            .WithMessage("Timezone must be a valid IANA timezone identifier.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThan(0).When(x => x.MaxCapacity.HasValue)
            .WithMessage("MaxCapacity must be greater than 0.");
    }

    private static bool BeValidIanaTimezone(string? timezone)
        => timezone is null || TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out _);
}
