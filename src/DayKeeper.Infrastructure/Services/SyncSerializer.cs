using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Domain.Mappings;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// System.Text.Json-based implementation of <see cref="ISyncSerializer"/>.
/// Excludes navigation properties and computed properties at runtime via a
/// <see cref="DefaultJsonTypeInfoResolver"/> modifier so that domain entities
/// remain free of serialization attributes.
/// </summary>
public sealed class SyncSerializer : ISyncSerializer
{
    private static readonly JsonSerializerOptions _options = BuildOptions();

    /// <inheritdoc />
    public JsonElement Serialize(BaseEntity entity)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(entity, entity.GetType(), _options);
        using var doc = JsonDocument.Parse(bytes);
        return doc.RootElement.Clone();
    }

    /// <inheritdoc />
    public BaseEntity Deserialize(JsonElement data, ChangeLogEntityType entityType)
    {
        var clrType = ChangeLogEntityTypeMap.GetClrType(entityType);
        return (BaseEntity)(JsonSerializer.Deserialize(data.GetRawText(), clrType, _options)
            ?? throw new JsonException($"Deserialization of {clrType.Name} returned null."));
    }

    private static JsonSerializerOptions BuildOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { ExcludeNavigationsAndComputed },
            },
        };
    }

    private static void ExcludeNavigationsAndComputed(JsonTypeInfo typeInfo)
    {
        if (!typeof(BaseEntity).IsAssignableFrom(typeInfo.Type))
        {
            return;
        }

        for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
        {
            if (ShouldExclude(typeInfo.Properties[i]))
            {
                typeInfo.Properties.RemoveAt(i);
            }
        }
    }

    private static bool ShouldExclude(JsonPropertyInfo prop)
    {
        var memberName = (prop.AttributeProvider as MemberInfo)?.Name;

        // Computed properties excluded by CLR member name.
        if (memberName is nameof(BaseEntity.IsDeleted) or "IsSystem")
        {
            return true;
        }

        var propType = prop.PropertyType;

        // Reference navigations: property type IS-A BaseEntity.
        if (typeof(BaseEntity).IsAssignableFrom(propType))
        {
            return true;
        }

        // Collection navigations: ICollection<T> where T : BaseEntity.
        if (propType.IsGenericType
            && propType.GetGenericTypeDefinition() == typeof(ICollection<>)
            && typeof(BaseEntity).IsAssignableFrom(propType.GetGenericArguments()[0]))
        {
            return true;
        }

        return false;
    }
}
