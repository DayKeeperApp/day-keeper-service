using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class CreateCalendarEventCommandValidatorTests
{
    private readonly CreateCalendarEventCommandValidator _validator = new();

    private static CreateCalendarEventCommand ValidCommand() =>
        new(
            CalendarId: Guid.NewGuid(),
            Title: "Team Standup",
            Description: "Daily sync",
            IsAllDay: false,
            StartAt: new DateTime(2026, 3, 1, 15, 0, 0, DateTimeKind.Utc),
            EndAt: new DateTime(2026, 3, 1, 15, 30, 0, DateTimeKind.Utc),
            StartDate: null,
            EndDate: null,
            Timezone: "America/Chicago",
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
    public async Task ValidInput_WithOptionalFields_PassesValidation()
    {
        var command = ValidCommand() with
        {
            RecurrenceRule = "FREQ=DAILY;COUNT=10",
            Location = "Conference Room A",
            EventTypeId = Guid.NewGuid(),
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── CalendarId: NotEmpty ─────────────────────────────────────────

    [Fact]
    public async Task CalendarId_Empty_FailsValidation()
    {
        var command = ValidCommand() with { CalendarId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CalendarId");
    }

    // ── Title: NotEmpty ─────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Title_Empty_FailsValidation(string? title)
    {
        var command = ValidCommand() with { Title = title! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    // ── Title: MaximumLength(512) ───────────────────────────────────

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

    // ── Timezone: NotEmpty ──────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Timezone_Empty_FailsValidation(string? timezone)
    {
        var command = ValidCommand() with { Timezone = timezone! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Timezone");
    }

    // ── Timezone: MaximumLength(64) ─────────────────────────────────

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
