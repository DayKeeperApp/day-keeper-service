using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

public sealed class UpdateListItemCommandValidator : AbstractValidator<UpdateListItemCommand>
{
    public UpdateListItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(256).When(x => x.Name is not null);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0).When(x => x.Quantity is not null);
        RuleFor(x => x.Unit).MaximumLength(32).When(x => x.Unit is not null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).When(x => x.SortOrder is not null);
    }
}
