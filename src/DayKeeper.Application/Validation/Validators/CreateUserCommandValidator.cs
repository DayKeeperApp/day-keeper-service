using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="CreateUserCommand"/> input before user creation.
/// </summary>
public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>TenantId</c> required; <c>DisplayName</c> required, max 256;
    /// <c>Email</c> required, max 320, valid format; <c>Timezone</c> required, max 64, valid IANA;
    /// <c>WeekStart</c> valid enum; <c>Locale</c> max 16 when provided.
    /// </summary>
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(320)
            .EmailAddress();

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .MaximumLength(64)
            .Must(BeValidIanaTimezone)
            .WithMessage("Timezone must be a valid IANA timezone identifier.");

        RuleFor(x => x.WeekStart)
            .IsInEnum();

        RuleFor(x => x.Locale)
            .MaximumLength(16)
            .When(x => x.Locale is not null);
    }

    private static bool BeValidIanaTimezone(string timezone)
    {
        if (string.IsNullOrEmpty(timezone))
            return false;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
    }
}
