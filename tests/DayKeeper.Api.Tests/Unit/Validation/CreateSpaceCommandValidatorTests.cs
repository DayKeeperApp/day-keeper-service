using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class CreateSpaceCommandValidatorTests
{
    private readonly CreateSpaceCommandValidator _validator = new();

    private static CreateSpaceCommand ValidCommand() =>
        new(Guid.NewGuid(), "My Space", SpaceType.Shared, Guid.NewGuid());

    // ── Valid input ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidInput_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(SpaceType.Personal)]
    [InlineData(SpaceType.Shared)]
    [InlineData(SpaceType.System)]
    public async Task ValidInput_AllSpaceTypeValues_PassValidation(SpaceType spaceType)
    {
        var command = ValidCommand() with { SpaceType = spaceType };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── TenantId: NotEmpty ────────────────────────────────────────────

    [Fact]
    public async Task TenantId_Empty_FailsValidation()
    {
        var command = ValidCommand() with { TenantId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    // ── Name: NotEmpty ────────────────────────────────────────────────

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

    // ── Name: MaximumLength(256) ──────────────────────────────────────

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

    // ── SpaceType: IsInEnum ───────────────────────────────────────────

    [Fact]
    public async Task SpaceType_InvalidEnumValue_FailsValidation()
    {
        var command = ValidCommand() with { SpaceType = (SpaceType)99 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SpaceType");
    }

    // ── CreatedByUserId: NotEmpty ─────────────────────────────────────

    [Fact]
    public async Task CreatedByUserId_Empty_FailsValidation()
    {
        var command = ValidCommand() with { CreatedByUserId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CreatedByUserId");
    }
}
