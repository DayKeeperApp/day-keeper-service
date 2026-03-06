using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class UpdateListItemCommandValidatorTests
{
    private readonly UpdateListItemCommandValidator _validator = new();

    private static UpdateListItemCommand ValidCommand() =>
        new(Guid.NewGuid(), "Bread", 2m, "loaves", true, 1);

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
        var command = new UpdateListItemCommand(Guid.NewGuid(), null, null, null, null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── Id: NotEmpty ────────────────────────────────────────────────

    [Fact]
    public async Task Id_Empty_FailsValidation()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    // ── Name: MaximumLength(256) when not null ──────────────────────

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

    // ── Quantity: GreaterThanOrEqualTo(0) when not null ──────────────

    [Fact]
    public async Task Quantity_Null_PassesValidation()
    {
        var command = ValidCommand() with { Quantity = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

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

    // ── Unit: MaximumLength(32) when not null ────────────────────────

    [Fact]
    public async Task Unit_Null_PassesValidation()
    {
        var command = ValidCommand() with { Unit = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

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

    // ── SortOrder: GreaterThanOrEqualTo(0) when not null ─────────────

    [Fact]
    public async Task SortOrder_Null_PassesValidation()
    {
        var command = ValidCommand() with { SortOrder = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

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
