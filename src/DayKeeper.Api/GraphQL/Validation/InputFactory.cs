using System.Reflection;
using DayKeeper.Application.Validation.Commands;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.GraphQL.Validation;

/// <summary>
/// Maps mutation field names to factory functions that construct the corresponding
/// FluentValidation command record from the HC-generated input object.
/// HC mutation conventions produce dictionary-like input objects at runtime,
/// so we read values by key (camelCase) first, then fall back to reflection.
/// </summary>
internal static class InputFactory
{
    internal static object? TryCreate(string fieldName, object hcInput)
    {
        if (!Factories.TryGetValue(fieldName, out var factory))
            return null;

        return factory(hcInput);
    }

    private static T? Get<T>(object obj, string name)
    {
        // HC-generated input objects implement IReadOnlyDictionary with camelCase keys
        if (obj is IReadOnlyDictionary<string, object?> dict)
        {
            var camelKey = char.ToLowerInvariant(name[0]) + name[1..];
            if (dict.TryGetValue(camelKey, out var value) && value is not null)
                return (T)value;
            if (dict.TryGetValue(name, out value) && value is not null)
                return (T)value;
            return default;
        }

        // Fallback: CLR-backed input types
        var prop = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (prop is null)
            return default;
        var propValue = prop.GetValue(obj);
        return propValue is null ? default : (T)propValue;
    }

    private static T GetRequired<T>(object obj, string name)
    {
        if (obj is IReadOnlyDictionary<string, object?> dict)
        {
            var camelKey = char.ToLowerInvariant(name[0]) + name[1..];
            if (dict.TryGetValue(camelKey, out var value) && value is not null)
                return (T)value;
            if (dict.TryGetValue(name, out value) && value is not null)
                return (T)value;
            throw new InvalidOperationException(
                $"Required field '{name}' (or '{camelKey}') not found or null in input dictionary.");
        }

        var prop = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Property '{name}' not found on {obj.GetType().Name}.");
        return (T)prop.GetValue(obj)!;
    }

    private static readonly Dictionary<string, Func<object, object>> Factories = new(StringComparer.Ordinal)
    {
        ["createTenant"] = input => new CreateTenantCommand(
            GetRequired<string>(input, "Name"),
            GetRequired<string>(input, "Slug")),

        ["updateTenant"] = input => new UpdateTenantCommand(
            GetRequired<Guid>(input, "Id"),
            Get<string>(input, "Name"),
            Get<string>(input, "Slug")),

        ["createUser"] = input => new CreateUserCommand(
            GetRequired<Guid>(input, "TenantId"),
            GetRequired<string>(input, "DisplayName"),
            GetRequired<string>(input, "Email"),
            GetRequired<string>(input, "Timezone"),
            GetRequired<WeekStart>(input, "WeekStart"),
            Get<string>(input, "Locale")),

        ["updateUser"] = input => new UpdateUserCommand(
            GetRequired<Guid>(input, "Id"),
            Get<string>(input, "DisplayName"),
            Get<string>(input, "Email"),
            Get<string>(input, "Timezone"),
            Get<WeekStart?>(input, "WeekStart"),
            Get<string>(input, "Locale")),

        ["createSpace"] = input => new CreateSpaceCommand(
            GetRequired<Guid>(input, "TenantId"),
            GetRequired<string>(input, "Name"),
            GetRequired<SpaceType>(input, "SpaceType"),
            GetRequired<Guid>(input, "CreatedByUserId")),

        ["updateSpace"] = input => new UpdateSpaceCommand(
            GetRequired<Guid>(input, "Id"),
            Get<string>(input, "Name"),
            Get<SpaceType?>(input, "SpaceType")),

        ["addSpaceMember"] = input => new AddSpaceMemberCommand(
            GetRequired<Guid>(input, "SpaceId"),
            GetRequired<Guid>(input, "UserId"),
            GetRequired<SpaceRole>(input, "Role")),

        ["updateSpaceMemberRole"] = input => new UpdateSpaceMemberRoleCommand(
            GetRequired<Guid>(input, "SpaceId"),
            GetRequired<Guid>(input, "UserId"),
            GetRequired<SpaceRole>(input, "NewRole")),
    };
}
