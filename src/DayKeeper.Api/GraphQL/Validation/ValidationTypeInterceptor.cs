using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace DayKeeper.Api.GraphQL.Validation;

/// <summary>
/// Type interceptor that automatically wires <see cref="ValidationMiddleware"/>
/// into all mutation fields, ensuring every mutation is validated with no opt-out.
/// </summary>
public sealed class ValidationTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is not ObjectTypeDefinition objectTypeDef)
            return;

        // Only intercept the Mutation root type
        if (!string.Equals(objectTypeDef.Name, "Mutation", StringComparison.Ordinal))
            return;

        foreach (var field in objectTypeDef.Fields)
        {
            if (field.IsIntrospectionField)
                continue;

            field.MiddlewareDefinitions.Insert(0,
                new FieldMiddlewareDefinition(
                    next => async context =>
                    {
                        var middleware = new ValidationMiddleware(next);
                        await middleware.InvokeAsync(context).ConfigureAwait(false);
                    }));
        }
    }
}
