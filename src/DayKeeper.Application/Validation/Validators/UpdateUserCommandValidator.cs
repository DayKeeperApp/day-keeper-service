using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.DisplayName)
            .MaximumLength(256)
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.Email)
            .MaximumLength(320)
            .EmailAddress()
            .When(x => x.Email is not null);

        RuleFor(x => x.Timezone)
            .MaximumLength(64)
            .Must(BeValidIanaTimezone!)
            .WithMessage("Timezone must be a valid IANA timezone identifier.")
            .When(x => x.Timezone is not null);

        RuleFor(x => x.WeekStart)
            .IsInEnum()
            .When(x => x.WeekStart is not null);

        RuleFor(x => x.Locale)
            .MaximumLength(16)
            .When(x => x.Locale is not null);
    }

    private static bool BeValidIanaTimezone(string timezone)
    {
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
