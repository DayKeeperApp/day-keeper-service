using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class UpdateContactMethodCommandValidatorTests
{
    private readonly UpdateContactMethodCommandValidator _validator = new();

    private static UpdateContactMethodCommand ValidCommand() =>
        new(Guid.NewGuid(), ContactMethodType.Phone, "555-1234", null, false);

    [Fact]
    public async Task ValidInput_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

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

    // ── Type: IsInEnum when provided ────────────────────────────────

    [Theory]
    [InlineData(ContactMethodType.Phone)]
    [InlineData(ContactMethodType.Email)]
    [InlineData(ContactMethodType.Other)]
    public async Task Type_ValidEnumValues_PassValidation(ContactMethodType type)
    {
        var command = ValidCommand() with { Type = type };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Type_InvalidEnumValue_FailsValidation()
    {
        var command = ValidCommand() with { Type = (ContactMethodType)99 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public async Task Type_Null_SkipsValidation()
    {
        var command = ValidCommand() with { Type = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── Value: MaximumLength(512) when provided ─────────────────────

    [Fact]
    public async Task Value_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Value = new string('a', 512) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Value_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Value = new string('a', 513) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Value");
    }

    [Fact]
    public async Task Value_Null_SkipsValidation()
    {
        var command = ValidCommand() with { Value = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── Label: MaximumLength(128) when provided ─────────────────────

    [Fact]
    public async Task Label_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Label = new string('a', 128) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Label_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Label = new string('a', 129) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Label");
    }

    [Fact]
    public async Task Label_Null_SkipsValidation()
    {
        var command = ValidCommand() with { Label = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
