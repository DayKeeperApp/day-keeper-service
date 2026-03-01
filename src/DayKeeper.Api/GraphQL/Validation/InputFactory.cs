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

        ["createCalendar"] = ctx => new CreateCalendarCommand(
            ctx.ArgumentValue<Guid>("spaceId"),
            ctx.ArgumentValue<string>("name"),
            ctx.ArgumentValue<string>("color"),
            ctx.ArgumentValue<bool>("isDefault")),

        ["updateCalendar"] = ctx => new UpdateCalendarCommand(
            ctx.ArgumentValue<Guid>("id"),
            ctx.ArgumentOptional<string?>("name") is { HasValue: true, Value: var ucn } ? ucn : null,
            ctx.ArgumentOptional<string?>("color") is { HasValue: true, Value: var ucc } ? ucc : null,
            ctx.ArgumentOptional<bool?>("isDefault") is { HasValue: true, Value: var ucd } ? ucd : null),

        ["createCalendarEvent"] = ctx => new CreateCalendarEventCommand(
            ctx.ArgumentValue<Guid>("calendarId"),
            ctx.ArgumentValue<string>("title"),
            ctx.ArgumentOptional<string?>("description") is { HasValue: true, Value: var cced } ? cced : null,
            ctx.ArgumentValue<bool>("isAllDay"),
            ctx.ArgumentValue<DateTime>("startAt"),
            ctx.ArgumentValue<DateTime>("endAt"),
            ctx.ArgumentOptional<DateOnly?>("startDate") is { HasValue: true, Value: var ccesd } ? ccesd : null,
            ctx.ArgumentOptional<DateOnly?>("endDate") is { HasValue: true, Value: var cceed } ? cceed : null,
            ctx.ArgumentValue<string>("timezone"),
            ctx.ArgumentOptional<string?>("recurrenceRule") is { HasValue: true, Value: var ccerr } ? ccerr : null,
            ctx.ArgumentOptional<DateTime?>("recurrenceEndAt") is { HasValue: true, Value: var ccerea } ? ccerea : null,
            ctx.ArgumentOptional<string?>("location") is { HasValue: true, Value: var ccel } ? ccel : null,
            ctx.ArgumentOptional<Guid?>("eventTypeId") is { HasValue: true, Value: var cceet } ? cceet : null),

        ["updateCalendarEvent"] = ctx => new UpdateCalendarEventCommand(
            ctx.ArgumentValue<Guid>("id"),
            ctx.ArgumentOptional<string?>("title") is { HasValue: true, Value: var ucet } ? ucet : null,
            ctx.ArgumentOptional<string?>("description") is { HasValue: true, Value: var uced } ? uced : null,
            ctx.ArgumentOptional<bool?>("isAllDay") is { HasValue: true, Value: var ucead } ? ucead : null,
            ctx.ArgumentOptional<DateTime?>("startAt") is { HasValue: true, Value: var ucesa } ? ucesa : null,
            ctx.ArgumentOptional<DateTime?>("endAt") is { HasValue: true, Value: var uceea } ? uceea : null,
            ctx.ArgumentOptional<DateOnly?>("startDate") is { HasValue: true, Value: var ucesd } ? ucesd : null,
            ctx.ArgumentOptional<DateOnly?>("endDate") is { HasValue: true, Value: var uceed } ? uceed : null,
            ctx.ArgumentOptional<string?>("timezone") is { HasValue: true, Value: var ucetz } ? ucetz : null,
            ctx.ArgumentOptional<string?>("recurrenceRule") is { HasValue: true, Value: var ucerr } ? ucerr : null,
            ctx.ArgumentOptional<DateTime?>("recurrenceEndAt") is { HasValue: true, Value: var ucerea } ? ucerea : null,
            ctx.ArgumentOptional<string?>("location") is { HasValue: true, Value: var ucel } ? ucel : null,
            ctx.ArgumentOptional<Guid?>("eventTypeId") is { HasValue: true, Value: var uceet } ? uceet : null),

        ["createPerson"] = ctx => new CreatePersonCommand(
            ctx.ArgumentValue<Guid>("spaceId"),
            ctx.ArgumentValue<string>("firstName"),
            ctx.ArgumentValue<string>("lastName"),
            ctx.ArgumentOptional<string?>("notes") is { HasValue: true, Value: var cpn } ? cpn : null),

        ["updatePerson"] = ctx => new UpdatePersonCommand(
            ctx.ArgumentValue<Guid>("id"),
            ctx.ArgumentOptional<string?>("firstName") is { HasValue: true, Value: var upfn } ? upfn : null,
            ctx.ArgumentOptional<string?>("lastName") is { HasValue: true, Value: var upln } ? upln : null,
            ctx.ArgumentOptional<string?>("notes") is { HasValue: true, Value: var upn } ? upn : null),

        ["createContactMethod"] = ctx => new CreateContactMethodCommand(
            ctx.ArgumentValue<Guid>("personId"),
            ctx.ArgumentValue<ContactMethodType>("type"),
            ctx.ArgumentValue<string>("value"),
            ctx.ArgumentOptional<string?>("label") is { HasValue: true, Value: var ccml } ? ccml : null,
            ctx.ArgumentValue<bool>("isPrimary")),

        ["updateContactMethod"] = ctx => new UpdateContactMethodCommand(
            ctx.ArgumentValue<Guid>("id"),
            ctx.ArgumentOptional<ContactMethodType?>("type") is { HasValue: true, Value: var ucmt } ? ucmt : null,
            ctx.ArgumentOptional<string?>("value") is { HasValue: true, Value: var ucmv } ? ucmv : null,
            ctx.ArgumentOptional<string?>("label") is { HasValue: true, Value: var ucml } ? ucml : null,
            ctx.ArgumentOptional<bool?>("isPrimary") is { HasValue: true, Value: var ucmp } ? ucmp : null),

        ["createAddress"] = ctx => new CreateAddressCommand(
            ctx.ArgumentValue<Guid>("personId"),
            ctx.ArgumentOptional<string?>("label") is { HasValue: true, Value: var cal } ? cal : null,
            ctx.ArgumentValue<string>("street1"),
            ctx.ArgumentOptional<string?>("street2") is { HasValue: true, Value: var cas2 } ? cas2 : null,
            ctx.ArgumentValue<string>("city"),
            ctx.ArgumentOptional<string?>("state") is { HasValue: true, Value: var cast } ? cast : null,
            ctx.ArgumentOptional<string?>("postalCode") is { HasValue: true, Value: var capc } ? capc : null,
            ctx.ArgumentValue<string>("country"),
            ctx.ArgumentValue<bool>("isPrimary")),

        ["updateAddress"] = ctx => new UpdateAddressCommand(
            ctx.ArgumentValue<Guid>("id"),
            ctx.ArgumentOptional<string?>("label") is { HasValue: true, Value: var ual } ? ual : null,
            ctx.ArgumentOptional<string?>("street1") is { HasValue: true, Value: var uas1 } ? uas1 : null,
            ctx.ArgumentOptional<string?>("street2") is { HasValue: true, Value: var uas2 } ? uas2 : null,
            ctx.ArgumentOptional<string?>("city") is { HasValue: true, Value: var uac } ? uac : null,
            ctx.ArgumentOptional<string?>("state") is { HasValue: true, Value: var uast } ? uast : null,
            ctx.ArgumentOptional<string?>("postalCode") is { HasValue: true, Value: var uapc } ? uapc : null,
            ctx.ArgumentOptional<string?>("country") is { HasValue: true, Value: var uaco } ? uaco : null,
            ctx.ArgumentOptional<bool?>("isPrimary") is { HasValue: true, Value: var uap } ? uap : null),

        ["createImportantDate"] = ctx => new CreateImportantDateCommand(
            ctx.ArgumentValue<Guid>("personId"),
            ctx.ArgumentValue<string>("label"),
            ctx.ArgumentValue<DateOnly>("dateValue"),
            ctx.ArgumentOptional<Guid?>("eventTypeId") is { HasValue: true, Value: var cidet } ? cidet : null),

        ["updateImportantDate"] = ctx => new UpdateImportantDateCommand(
            ctx.ArgumentValue<Guid>("id"),
            ctx.ArgumentOptional<string?>("label") is { HasValue: true, Value: var uidl } ? uidl : null,
            ctx.ArgumentOptional<DateOnly?>("dateValue") is { HasValue: true, Value: var uidd } ? uidd : null,
            ctx.ArgumentOptional<Guid?>("eventTypeId") is { HasValue: true, Value: var uidet } ? uidet : null),
    };
}
