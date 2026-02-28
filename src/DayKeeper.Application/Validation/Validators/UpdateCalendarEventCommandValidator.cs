using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="UpdateCalendarEventCommand"/> input before event updates.
/// </summary>
public sealed class UpdateCalendarEventCommandValidator : AbstractValidator<UpdateCalendarEventCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>Id</c> required; <c>Title</c> max 512 when provided;
    /// <c>Timezone</c> max 64 when provided; <c>RecurrenceRule</c> max 512 when provided;
    /// <c>Location</c> max 512 when provided.
    /// </summary>
    public UpdateCalendarEventCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Title)
            .MaximumLength(512)
            .When(x => x.Title is not null);

        RuleFor(x => x.Timezone)
            .MaximumLength(64)
            .When(x => x.Timezone is not null);

        RuleFor(x => x.RecurrenceRule)
            .MaximumLength(512)
            .When(x => x.RecurrenceRule is not null);

        RuleFor(x => x.Location)
            .MaximumLength(512)
            .When(x => x.Location is not null);
    }
}
