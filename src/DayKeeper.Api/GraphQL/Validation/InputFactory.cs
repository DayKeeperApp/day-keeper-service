using DayKeeper.Application.Validation.Commands;
using DayKeeper.Domain.Enums;
using HotChocolate.Resolvers;


namespace DayKeeper.Api.GraphQL.Validation;

/// <summary>
/// Maps mutation field names to factory functions that construct the corresponding
/// FluentValidation command record from the HC middleware context.
/// After HC's ArgumentMiddleware unpacks the <c>input</c> wrapper, individual
/// scalar arguments are accessible directly via <see cref="IResolverContext.ArgumentValue{T}"/>.
/// </summary>
internal static class InputFactory
{
    internal static object? TryCreate(string fieldName, IMiddlewareContext context)
    {
        if (!_factories.TryGetValue(fieldName, out var factory))
            return null;

        return factory(context);
    }

    private static readonly Dictionary<string, Func<IMiddlewareContext, object>> _factories = new(StringComparer.Ordinal)
    {
        ["createTenant"] = ctx => new CreateTenantCommand(
            ctx.ArgumentValue<string>("name"),
            ctx.ArgumentValue<string>("slug")),

        ["updateTenant"] = ctx => new UpdateTenantCommand(
            ctx.ArgumentValue<Guid>("id"),
            ctx.ArgumentOptional<string?>("name") is { HasValue: true, Value: var tn } ? tn : null,
            ctx.ArgumentOptional<string?>("slug") is { HasValue: true, Value: var ts } ? ts : null),

        ["createUser"] = ctx => new CreateUserCommand(
            ctx.ArgumentValue<Guid>("tenantId"),
            ctx.ArgumentValue<string>("displayName"),
            ctx.ArgumentValue<string>("email"),
            ctx.ArgumentValue<string>("timezone"),
            ctx.ArgumentValue<WeekStart>("weekStart"),
            ctx.ArgumentOptional<string?>("locale") is { HasValue: true, Value: var l } ? l : null),

        ["updateUser"] = ctx => new UpdateUserCommand(
            ctx.ArgumentValue<Guid>("id"),
            ctx.ArgumentOptional<string?>("displayName") is { HasValue: true, Value: var dn } ? dn : null,
            ctx.ArgumentOptional<string?>("email") is { HasValue: true, Value: var ue } ? ue : null,
            ctx.ArgumentOptional<string?>("timezone") is { HasValue: true, Value: var ut } ? ut : null,
            ctx.ArgumentOptional<WeekStart?>("weekStart") is { HasValue: true, Value: var uw } ? uw : null,
            ctx.ArgumentOptional<string?>("locale") is { HasValue: true, Value: var ul } ? ul : null),

        ["createSpace"] = ctx => new CreateSpaceCommand(
            ctx.ArgumentValue<Guid>("tenantId"),
            ctx.ArgumentValue<string>("name"),
            ctx.ArgumentValue<SpaceType>("spaceType"),
            ctx.ArgumentValue<Guid>("createdByUserId")),

        ["updateSpace"] = ctx => new UpdateSpaceCommand(
            ctx.ArgumentValue<Guid>("id"),
            ctx.ArgumentOptional<string?>("name") is { HasValue: true, Value: var sn } ? sn : null,
            ctx.ArgumentOptional<SpaceType?>("spaceType") is { HasValue: true, Value: var st } ? st : null),

        ["addSpaceMember"] = ctx => new AddSpaceMemberCommand(
            ctx.ArgumentValue<Guid>("spaceId"),
            ctx.ArgumentValue<Guid>("userId"),
            ctx.ArgumentValue<SpaceRole>("role")),

        ["updateSpaceMemberRole"] = ctx => new UpdateSpaceMemberRoleCommand(
            ctx.ArgumentValue<Guid>("spaceId"),
            ctx.ArgumentValue<Guid>("userId"),
            ctx.ArgumentValue<SpaceRole>("newRole")),

        ["createProject"] = ctx => new CreateProjectCommand(
            ctx.ArgumentValue<Guid>("spaceId"),
            ctx.ArgumentValue<string>("name"),
            ctx.ArgumentOptional<string?>("description") is { HasValue: true, Value: var cpd } ? cpd : null),

        ["updateProject"] = ctx => new UpdateProjectCommand(
            ctx.ArgumentValue<Guid>("id"),
            ctx.ArgumentOptional<string?>("name") is { HasValue: true, Value: var upn } ? upn : null,
            ctx.ArgumentOptional<string?>("description") is { HasValue: true, Value: var upd } ? upd : null),

        ["createTaskItem"] = ctx => new CreateTaskItemCommand(
            ctx.ArgumentValue<Guid>("spaceId"),
            ctx.ArgumentValue<string>("title"),
            ctx.ArgumentOptional<string?>("description") is { HasValue: true, Value: var ctd } ? ctd : null,
            ctx.ArgumentOptional<Guid?>("projectId") is { HasValue: true, Value: var ctp } ? ctp : null,
            ctx.ArgumentValue<TaskItemStatus>("status"),
            ctx.ArgumentValue<TaskItemPriority>("priority"),
            ctx.ArgumentOptional<DateTime?>("dueAt") is { HasValue: true, Value: var ctda } ? ctda : null,
            ctx.ArgumentOptional<DateOnly?>("dueDate") is { HasValue: true, Value: var ctdd } ? ctdd : null,
            ctx.ArgumentOptional<string?>("recurrenceRule") is { HasValue: true, Value: var ctr } ? ctr : null),

        ["updateTaskItem"] = ctx => new UpdateTaskItemCommand(
            ctx.ArgumentValue<Guid>("id"),
            ctx.ArgumentOptional<string?>("title") is { HasValue: true, Value: var utt } ? utt : null,
            ctx.ArgumentOptional<string?>("description") is { HasValue: true, Value: var utd } ? utd : null,
            ctx.ArgumentOptional<TaskItemStatus?>("status") is { HasValue: true, Value: var uts } ? uts : null,
            ctx.ArgumentOptional<TaskItemPriority?>("priority") is { HasValue: true, Value: var utp } ? utp : null,
            ctx.ArgumentOptional<Guid?>("projectId") is { HasValue: true, Value: var utpj } ? utpj : null,
            ctx.ArgumentOptional<DateTime?>("dueAt") is { HasValue: true, Value: var utda } ? utda : null,
            ctx.ArgumentOptional<DateOnly?>("dueDate") is { HasValue: true, Value: var utdd } ? utdd : null,
            ctx.ArgumentOptional<string?>("recurrenceRule") is { HasValue: true, Value: var utr } ? utr : null),
    };
}
