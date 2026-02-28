using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class AddSpaceMemberCommandValidatorTests
{
    private readonly AddSpaceMemberCommandValidator _validator = new();

    private static AddSpaceMemberCommand ValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), SpaceRole.Editor);

    // ── Valid input ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidInput_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(SpaceRole.Viewer)]
    [InlineData(SpaceRole.Editor)]
    [InlineData(SpaceRole.Owner)]
    public async Task ValidInput_AllRoleValues_PassValidation(SpaceRole role)
    {
        var command = ValidCommand() with { Role = role };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── SpaceId: NotEmpty ─────────────────────────────────────────────

    [Fact]
    public async Task SpaceId_Empty_FailsValidation()
    {
        var command = ValidCommand() with { SpaceId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SpaceId");
    }

    // ── UserId: NotEmpty ──────────────────────────────────────────────

    [Fact]
    public async Task UserId_Empty_FailsValidation()
    {
        var command = ValidCommand() with { UserId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    // ── Role: IsInEnum ────────────────────────────────────────────────

    [Fact]
    public async Task Role_InvalidEnumValue_FailsValidation()
    {
        var command = ValidCommand() with { Role = (SpaceRole)99 };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    // ── Multiple violations ───────────────────────────────────────────

    [Fact]
    public async Task AllFieldsInvalid_ReportsAllViolations()
    {
        var command = new AddSpaceMemberCommand(Guid.Empty, Guid.Empty, (SpaceRole)99);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SpaceId");
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }
}
