using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="UpdateUserCommand"/> input before user updates.
/// </summary>
public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>Id</c> required; <c>DisplayName</c> max 256 when provided;
    /// <c>Email</c> max 320, valid format when provided; <c>Timezone</c> max 64, valid IANA when provided;
    /// <c>WeekStart</c> valid enum when provided; <c>Locale</c> max 16 when provided.
    /// </summary>
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
