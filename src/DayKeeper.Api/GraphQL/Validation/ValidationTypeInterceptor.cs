using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace DayKeeper.Api.GraphQL.Validation;

/// <summary>
/// Type interceptor that automatically wires <see cref="ValidationMiddleware"/>
/// into all mutation fields, ensuring every mutation is validated with no opt-out.
/// The middleware is inserted just before the ArgumentMiddleware (which is the last
/// middleware added by HC's mutation conventions), placing it inside ErrorMiddleware's
/// try-catch scope so that <c>InputValidationException</c> is correctly mapped to the
/// mutation error union, while the "input" argument is still accessible.
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

            var middleware = new FieldMiddlewareDefinition(
                next => async context =>
                {
                    var mw = new ValidationMiddleware(next);
                    await mw.InvokeAsync(context).ConfigureAwait(false);
                });

            // HC's MutationConventionTypeInterceptor adds [Result, Error, Argument]
            // at positions 0-2. We insert at the second-to-last position so we're
            // inside ErrorMiddleware's scope but before ArgumentMiddleware unwraps the input.
            var count = field.MiddlewareDefinitions.Count;
            if (count >= 2)
            {
                // Insert before the last entry (ArgumentMiddleware)
                field.MiddlewareDefinitions.Insert(count - 1, middleware);
            }
            else
            {
                // Fallback: no convention middleware; insert at end
                field.MiddlewareDefinitions.Add(middleware);
            }
        }
    }
}
