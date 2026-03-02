using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

public sealed class CreateListItemCommandValidator : AbstractValidator<CreateListItemCommand>
{
    public CreateListItemCommandValidator()
    {
        RuleFor(x => x.ShoppingListId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Unit).MaximumLength(32).When(x => x.Unit is not null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
