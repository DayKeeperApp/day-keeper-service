using DayKeeper.Application.Validation.Commands;
using DayKeeper.Application.Validation.Validators;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.Tests.Unit.Validation;

public sealed class UpdateSpaceCommandValidatorTests
{
    private readonly UpdateSpaceCommandValidator _validator = new();

    // ── Valid input ───────────────────────────────────────────────────

    [Fact]
    public async Task ValidInput_AllFieldsProvided_PassesValidation()
    {
        var command = new UpdateSpaceCommand(Guid.NewGuid(), "Updated Space", SpaceType.Shared);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidInput_OptionalFieldsNull_PassesValidation()
    {
        var command = new UpdateSpaceCommand(Guid.NewGuid(), null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── Id: NotEmpty ──────────────────────────────────────────────────

    [Fact]
    public async Task Id_Empty_FailsValidation()
    {
        var command = new UpdateSpaceCommand(Guid.Empty, "Valid Name", SpaceType.Shared);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    // ── Name: MaximumLength(256) when not null ────────────────────────

    [Fact]
    public async Task Name_AtMaxLength_PassesValidation()
    {
        var command = new UpdateSpaceCommand(Guid.NewGuid(), new string('a', 256), null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Name_ExceedsMaxLength_FailsValidation()
    {
        var command = new UpdateSpaceCommand(Guid.NewGuid(), new string('a', 257), null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_Null_SkipsValidation()
    {
        var command = new UpdateSpaceCommand(Guid.NewGuid(), null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == "Name");
    }

    // ── SpaceType: IsInEnum when not null ─────────────────────────────

    [Theory]
    [InlineData(SpaceType.Personal)]
    [InlineData(SpaceType.Shared)]
    [InlineData(SpaceType.System)]
    public async Task SpaceType_ValidEnumValues_PassValidation(SpaceType spaceType)
    {
        var command = new UpdateSpaceCommand(Guid.NewGuid(), null, spaceType);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SpaceType_InvalidEnumValue_FailsValidation()
    {
        var command = new UpdateSpaceCommand(Guid.NewGuid(), null, (SpaceType)99);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SpaceType");
    }

    [Fact]
    public async Task SpaceType_Null_SkipsValidation()
    {
        var command = new UpdateSpaceCommand(Guid.NewGuid(), null, null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == "SpaceType");
    }
}
