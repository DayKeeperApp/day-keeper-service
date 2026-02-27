using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
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
