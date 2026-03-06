using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class CreateListItemCommandValidatorTests
{
    private readonly CreateListItemCommandValidator _validator = new();

    private static CreateListItemCommand ValidCommand() =>
        new(Guid.NewGuid(), "Milk", 1m, "gallon", 0);

    // ── Valid input ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidInput_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidInput_WithNullUnit_PassesValidation()
    {
        var command = ValidCommand() with { Unit = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── ShoppingListId: NotEmpty ────────────────────────────────────

    [Fact]
    public async Task ShoppingListId_Empty_FailsValidation()
    {
        var command = ValidCommand() with { ShoppingListId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShoppingListId");
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

    // ── Quantity: GreaterThanOrEqualTo(0) ────────────────────────────

    [Fact]
    public async Task Quantity_Zero_PassesValidation()
    {
        var command = ValidCommand() with { Quantity = 0m };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Quantity_Negative_FailsValidation()
    {
        var command = ValidCommand() with { Quantity = -1m };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    // ── Unit: MaximumLength(32) ─────────────────────────────────────

    [Fact]
    public async Task Unit_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Unit = new string('a', 32) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Unit_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Unit = new string('a', 33) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Unit");
    }

    // ── SortOrder: GreaterThanOrEqualTo(0) ──────────────────────────

    [Fact]
    public async Task SortOrder_Zero_PassesValidation()
    {
        var command = ValidCommand() with { SortOrder = 0 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SortOrder_Negative_FailsValidation()
    {
        var command = ValidCommand() with { SortOrder = -1 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SortOrder");
    }
}
