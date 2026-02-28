using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="CreateCalendarEventCommand"/> input before event creation.
/// </summary>
public sealed class CreateCalendarEventCommandValidator : AbstractValidator<CreateCalendarEventCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>CalendarId</c> required; <c>Title</c> required, max 512;
    /// <c>Timezone</c> required, max 64; <c>RecurrenceRule</c> max 512 when provided;
    /// <c>Location</c> max 512 when provided.
    /// </summary>
    public CreateCalendarEventCommandValidator()
    {
        RuleFor(x => x.CalendarId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(512);

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.RecurrenceRule)
            .MaximumLength(512)
            .When(x => x.RecurrenceRule is not null);

        RuleFor(x => x.Location)
            .MaximumLength(512)
            .When(x => x.Location is not null);
    }
}
