using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

public sealed class UpdateTaskItemCommandValidator : AbstractValidator<UpdateTaskItemCommand>
{
    public UpdateTaskItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Title)
            .MaximumLength(512)
            .When(x => x.Title is not null);

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status is not null);

        RuleFor(x => x.Priority)
            .IsInEnum()
            .When(x => x.Priority is not null);

        RuleFor(x => x.RecurrenceRule)
            .MaximumLength(1024)
            .When(x => x.RecurrenceRule is not null);
    }
}
