using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class UpdateCalendarEventCommandValidatorTests
{
    private readonly UpdateCalendarEventCommandValidator _validator = new();

    private static UpdateCalendarEventCommand ValidCommand() =>
        new(
            Id: Guid.NewGuid(),
            Title: "Updated Standup",
            Description: null,
            IsAllDay: null,
            StartAt: null,
            EndAt: null,
            StartDate: null,
            EndDate: null,
            Timezone: null,
            RecurrenceRule: null,
            RecurrenceEndAt: null,
            Location: null,
            EventTypeId: null);

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
        var command = ValidCommand() with { Title = null };

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

    // ── Title: MaximumLength(512) when provided ─────────────────────

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

    // ── Timezone: MaximumLength(64) when provided ───────────────────

    [Fact]
    public async Task Timezone_Null_PassesValidation()
    {
        var command = ValidCommand() with { Timezone = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Timezone_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Timezone = new string('a', 65) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Timezone");
    }

    // ── RecurrenceRule: MaximumLength(512) when provided ─────────────

    [Fact]
    public async Task RecurrenceRule_Null_PassesValidation()
    {
        var command = ValidCommand() with { RecurrenceRule = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RecurrenceRule_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { RecurrenceRule = new string('a', 513) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecurrenceRule");
    }

    // ── Location: MaximumLength(512) when provided ──────────────────

    [Fact]
    public async Task Location_Null_PassesValidation()
    {
        var command = ValidCommand() with { Location = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Location_ExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand() with { Location = new string('a', 513) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Location");
    }
}
