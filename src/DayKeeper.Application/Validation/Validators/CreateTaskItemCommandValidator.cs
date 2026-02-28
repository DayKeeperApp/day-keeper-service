using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

/// <summary>
/// Validates the <see cref="CreateTaskItemCommand"/> input before task item creation.
/// </summary>
public sealed class CreateTaskItemCommandValidator : AbstractValidator<CreateTaskItemCommand>
{
    /// <summary>
    /// Initializes validation rules: <c>SpaceId</c> required; <c>Title</c> required, max 512;
    /// <c>Description</c> max 4000 when provided; <c>Status</c> valid enum; <c>Priority</c> valid enum;
    /// <c>RecurrenceRule</c> max 1024 when provided.
    /// </summary>
    public CreateTaskItemCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(512);

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.Priority)
            .IsInEnum();

        RuleFor(x => x.RecurrenceRule)
            .MaximumLength(1024)
            .When(x => x.RecurrenceRule is not null);
    }
}
