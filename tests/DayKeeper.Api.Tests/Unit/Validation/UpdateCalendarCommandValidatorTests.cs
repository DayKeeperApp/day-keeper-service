using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class UpdateCalendarCommandValidatorTests
{
    private readonly UpdateCalendarCommandValidator _validator = new();

    private static UpdateCalendarCommand ValidCommand() =>
        new(Guid.NewGuid(), "Updated Calendar", "#FF5733", true);

    // ── Valid input ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidInput_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidInput_AllNullOptionalFields_PassesValidation()
    {
        var command = new UpdateCalendarCommand(Guid.NewGuid(), null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── Id: NotEmpty ─────────────────────────────────────────────────

    [Fact]
    public async Task Id_Empty_FailsValidation()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    // ── Name: MaximumLength(256) when provided ──────────────────────

    [Fact]
    public async Task Name_Null_PassesValidation()
    {
        var command = ValidCommand() with { Name = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Name_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Name = new string('a', 256) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Name_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Name = new string('a', 257) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    // ── Color: MaximumLength(16) when provided ──────────────────────

    [Fact]
    public async Task Color_Null_PassesValidation()
    {
        var command = ValidCommand() with { Color = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Color_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Color = new string('a', 17) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Color");
    }
}
