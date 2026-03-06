using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class CreateShoppingListCommandValidatorTests
{
    private readonly CreateShoppingListCommandValidator _validator = new();

    private static CreateShoppingListCommand ValidCommand() =>
        new(Guid.NewGuid(), "Groceries");

    // ── Valid input ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidInput_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    // ── SpaceId: NotEmpty ────────────────────────────────────────────

    [Fact]
    public async Task SpaceId_Empty_FailsValidation()
    {
        var command = ValidCommand() with { SpaceId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SpaceId");
    }

    // ── Name: NotEmpty ──────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Name_Empty_FailsValidation(string? name)
    {
        var command = ValidCommand() with { Name = name! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    // ── Name: MaximumLength(256) ────────────────────────────────────

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
}
