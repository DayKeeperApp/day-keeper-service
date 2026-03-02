using DayKeeper.Application.Validation.Commands;
using FluentValidation;

namespace DayKeeper.Application.Validation.Validators;

public sealed class UpdateDeviceCommandValidator : AbstractValidator<UpdateDeviceCommand>
{
    public UpdateDeviceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DeviceName).MaximumLength(256).When(x => x.DeviceName is not null);
        RuleFor(x => x.FcmToken).MaximumLength(4096).When(x => x.FcmToken is not null);
    }
}
