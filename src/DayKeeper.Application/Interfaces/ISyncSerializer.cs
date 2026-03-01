using System.Text.Json;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Serializes and deserializes <see cref="BaseEntity"/>-derived entities
/// to and from a JSON transport format for sync push/pull operations.
/// Navigation properties and computed properties are excluded automatically.
/// </summary>
public interface ISyncSerializer
{
    /// <summary>
    /// Serializes a <see cref="BaseEntity"/> to a <see cref="JsonElement"/>,
    /// excluding navigation properties and computed columns.
    /// Uses the runtime type of <paramref name="entity"/> for full property resolution.
    /// </summary>
    JsonElement Serialize(BaseEntity entity);

    /// <summary>
    /// Deserializes a <see cref="JsonElement"/> into a <see cref="BaseEntity"/>
    /// using the CLR type resolved from <paramref name="entityType"/>.
    /// </summary>
    /// <exception cref="JsonException">
    /// Thrown if the JSON does not represent a valid entity of the resolved type.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="entityType"/> has no CLR type mapping.
    /// </exception>
    BaseEntity Deserialize(JsonElement data, ChangeLogEntityType entityType);
}
