using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class UpdateTaskItemCommandValidatorTests
{
    private readonly UpdateTaskItemCommandValidator _validator = new();

    private static UpdateTaskItemCommand ValidCommand() =>
        new(Guid.NewGuid(), "Updated title", null, null, null, null, null, null, null);

    // ── Valid input ───────────────────────────────────────────────────

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

    // ── Title: MaximumLength(512) ────────────────────────────────────

    [Fact]
    public async Task Title_Null_PassesValidation()
    {
        var command = ValidCommand() with { Title = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Title_AtMaxLength_PassesValidation()
    {
        var command = ValidCommand() with { Title = new string('a', 512) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Title_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Title = new string('a', 513) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    // ── Description: MaximumLength(4000) ─────────────────────────────

    [Fact]
    public async Task Description_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Description = new string('a', 4001) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    // ── Status: IsInEnum ─────────────────────────────────────────────

    [Fact]
    public async Task Status_Null_PassesValidation()
    {
        var command = ValidCommand() with { Status = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Status_InvalidEnum_FailsValidation()
    {
        var command = ValidCommand() with { Status = (TaskItemStatus)99 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }

    // ── Priority: IsInEnum ───────────────────────────────────────────

    [Fact]
    public async Task Priority_InvalidEnum_FailsValidation()
    {
        var command = ValidCommand() with { Priority = (TaskItemPriority)99 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Priority");
    }

    // ── RecurrenceRule: MaximumLength(1024) ───────────────────────────

    [Fact]
    public async Task RecurrenceRule_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { RecurrenceRule = new string('R', 1025) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecurrenceRule");
    }
}
