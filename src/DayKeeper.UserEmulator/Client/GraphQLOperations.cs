namespace DayKeeper.UserEmulator.Client;

public static class GraphQLOperations
{
    // -------------------------------------------------------------------------
    // TENANT
    // -------------------------------------------------------------------------

    public const string CreateTenant = """
        mutation CreateTenant($input: CreateTenantInput!) {
            createTenant(input: $input) {
                tenant { id name slug }
            }
        }
        """;

    public const string DeleteTenant = """
        mutation DeleteTenant($input: DeleteTenantInput!) {
            deleteTenant(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // USER
    // -------------------------------------------------------------------------

    public const string CreateUser = """
        mutation CreateUser($input: CreateUserInput!) {
            createUser(input: $input) {
                user { id displayName email timezone weekStart locale }
            }
        }
        """;

    public const string DeleteUser = """
        mutation DeleteUser($input: DeleteUserInput!) {
            deleteUser(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // SPACE
    // -------------------------------------------------------------------------

    public const string CreateSpace = """
        mutation CreateSpace($input: CreateSpaceInput!) {
            createSpace(input: $input) {
                space { id name spaceType }
            }
        }
        """;

    public const string DeleteSpace = """
        mutation DeleteSpace($input: DeleteSpaceInput!) {
            deleteSpace(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // SPACE MEMBERSHIP
    // -------------------------------------------------------------------------

    public const string AddSpaceMember = """
        mutation AddSpaceMember($input: AddSpaceMemberInput!) {
            addSpaceMember(input: $input) {
                spaceMembership { id spaceId userId role }
            }
        }
        """;

    public const string UpdateSpaceMemberRole = """
        mutation UpdateSpaceMemberRole($input: UpdateSpaceMemberRoleInput!) {
            updateSpaceMemberRole(input: $input) {
                spaceMembership { id spaceId userId role }
            }
        }
        """;

    public const string RemoveSpaceMember = """
        mutation RemoveSpaceMember($input: RemoveSpaceMemberInput!) {
            removeSpaceMember(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // PROJECT
    // -------------------------------------------------------------------------

    public const string CreateProject = """
        mutation CreateProject($input: CreateProjectInput!) {
            createProject(input: $input) {
                project { id name description spaceId }
            }
        }
        """;

    public const string UpdateProject = """
        mutation UpdateProject($input: UpdateProjectInput!) {
            updateProject(input: $input) {
                project { id name description }
            }
        }
        """;

    public const string DeleteProject = """
        mutation DeleteProject($input: DeleteProjectInput!) {
            deleteProject(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // TASK ITEM
    // -------------------------------------------------------------------------

    public const string CreateTaskItem = """
        mutation CreateTaskItem($input: CreateTaskItemInput!) {
            createTaskItem(input: $input) {
                taskItem { id title status priority projectId spaceId dueAt dueDate completedAt }
            }
        }
        """;

    public const string UpdateTaskItem = """
        mutation UpdateTaskItem($input: UpdateTaskItemInput!) {
            updateTaskItem(input: $input) {
                taskItem { id title status priority completedAt }
            }
        }
        """;

    public const string CompleteTaskItem = """
        mutation CompleteTaskItem($input: CompleteTaskItemInput!) {
            completeTaskItem(input: $input) {
                taskItem { id title status completedAt }
            }
        }
        """;

    public const string DeleteTaskItem = """
        mutation DeleteTaskItem($input: DeleteTaskItemInput!) {
            deleteTaskItem(input: $input) {
                boolean
            }
        }
        """;

    public const string AssignCategory = """
        mutation AssignCategory($input: AssignCategoryInput!) {
            assignCategory(input: $input) {
                taskItemCategory { id taskItemId categoryId }
            }
        }
        """;

    public const string RemoveCategory = """
        mutation RemoveCategory($input: RemoveCategoryInput!) {
            removeCategory(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // CALENDAR
    // -------------------------------------------------------------------------

    public const string CreateCalendar = """
        mutation CreateCalendar($input: CreateCalendarInput!) {
            createCalendar(input: $input) {
                calendar { id name color isDefault spaceId }
            }
        }
        """;

    public const string UpdateCalendar = """
        mutation UpdateCalendar($input: UpdateCalendarInput!) {
            updateCalendar(input: $input) {
                calendar { id name color isDefault }
            }
        }
        """;

    public const string DeleteCalendar = """
        mutation DeleteCalendar($input: DeleteCalendarInput!) {
            deleteCalendar(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // CALENDAR EVENT
    // -------------------------------------------------------------------------

    public const string CreateCalendarEvent = """
        mutation CreateCalendarEvent($input: CreateCalendarEventInput!) {
            createCalendarEvent(input: $input) {
                calendarEvent { id title isAllDay startAt endAt startDate endDate timezone recurrenceRule location calendarId }
            }
        }
        """;

    public const string UpdateCalendarEvent = """
        mutation UpdateCalendarEvent($input: UpdateCalendarEventInput!) {
            updateCalendarEvent(input: $input) {
                calendarEvent { id title isAllDay startAt endAt }
            }
        }
        """;

    public const string DeleteCalendarEvent = """
        mutation DeleteCalendarEvent($input: DeleteCalendarEventInput!) {
            deleteCalendarEvent(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // SHOPPING LIST
    // -------------------------------------------------------------------------

    public const string CreateShoppingList = """
        mutation CreateShoppingList($input: CreateShoppingListInput!) {
            createShoppingList(input: $input) {
                shoppingList { id name spaceId }
            }
        }
        """;

    public const string UpdateShoppingList = """
        mutation UpdateShoppingList($input: UpdateShoppingListInput!) {
            updateShoppingList(input: $input) {
                shoppingList { id name }
            }
        }
        """;

    public const string DeleteShoppingList = """
        mutation DeleteShoppingList($input: DeleteShoppingListInput!) {
            deleteShoppingList(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // LIST ITEM
    // -------------------------------------------------------------------------

    public const string CreateListItem = """
        mutation CreateListItem($input: CreateListItemInput!) {
            createListItem(input: $input) {
                listItem { id name quantity unit isChecked sortOrder shoppingListId }
            }
        }
        """;

    public const string UpdateListItem = """
        mutation UpdateListItem($input: UpdateListItemInput!) {
            updateListItem(input: $input) {
                listItem { id name quantity unit isChecked sortOrder }
            }
        }
        """;

    public const string DeleteListItem = """
        mutation DeleteListItem($input: DeleteListItemInput!) {
            deleteListItem(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // PERSON
    // -------------------------------------------------------------------------

    public const string CreatePerson = """
        mutation CreatePerson($input: CreatePersonInput!) {
            createPerson(input: $input) {
                person { id firstName lastName notes spaceId }
            }
        }
        """;

    public const string UpdatePerson = """
        mutation UpdatePerson($input: UpdatePersonInput!) {
            updatePerson(input: $input) {
                person { id firstName lastName notes }
            }
        }
        """;

    public const string DeletePerson = """
        mutation DeletePerson($input: DeletePersonInput!) {
            deletePerson(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // CONTACT METHOD
    // -------------------------------------------------------------------------

    public const string CreateContactMethod = """
        mutation CreateContactMethod($input: CreateContactMethodInput!) {
            createContactMethod(input: $input) {
                contactMethod { id personId type value label isPrimary }
            }
        }
        """;

    public const string UpdateContactMethod = """
        mutation UpdateContactMethod($input: UpdateContactMethodInput!) {
            updateContactMethod(input: $input) {
                contactMethod { id type value label isPrimary }
            }
        }
        """;

    public const string DeleteContactMethod = """
        mutation DeleteContactMethod($input: DeleteContactMethodInput!) {
            deleteContactMethod(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // ADDRESS
    // -------------------------------------------------------------------------

    public const string CreateAddress = """
        mutation CreateAddress($input: CreateAddressInput!) {
            createAddress(input: $input) {
                address { id personId label street1 street2 city state postalCode country isPrimary }
            }
        }
        """;

    public const string UpdateAddress = """
        mutation UpdateAddress($input: UpdateAddressInput!) {
            updateAddress(input: $input) {
                address { id label street1 city }
            }
        }
        """;

    public const string DeleteAddress = """
        mutation DeleteAddress($input: DeleteAddressInput!) {
            deleteAddress(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // IMPORTANT DATE
    // -------------------------------------------------------------------------

    public const string CreateImportantDate = """
        mutation CreateImportantDate($input: CreateImportantDateInput!) {
            createImportantDate(input: $input) {
                importantDate { id personId label date }
            }
        }
        """;

    public const string UpdateImportantDate = """
        mutation UpdateImportantDate($input: UpdateImportantDateInput!) {
            updateImportantDate(input: $input) {
                importantDate { id label date }
            }
        }
        """;

    public const string DeleteImportantDate = """
        mutation DeleteImportantDate($input: DeleteImportantDateInput!) {
            deleteImportantDate(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // DEVICE
    // -------------------------------------------------------------------------

    public const string CreateDevice = """
        mutation CreateDevice($input: CreateDeviceInput!) {
            createDevice(input: $input) {
                device { id userId deviceName platform fcmToken }
            }
        }
        """;

    public const string UpdateDevice = """
        mutation UpdateDevice($input: UpdateDeviceInput!) {
            updateDevice(input: $input) {
                device { id deviceName fcmToken lastSyncAt }
            }
        }
        """;

    public const string DeleteDevice = """
        mutation DeleteDevice($input: DeleteDeviceInput!) {
            deleteDevice(input: $input) {
                boolean
            }
        }
        """;

    // -------------------------------------------------------------------------
    // DEVICE NOTIFICATION PREFERENCE
    // -------------------------------------------------------------------------

    public const string UpdateDeviceNotificationPreference = """
        mutation UpdateDeviceNotificationPreference($input: UpdateDeviceNotificationPreferenceInput!) {
            updateDeviceNotificationPreference(input: $input) {
                deviceNotificationPreference { deviceId dndEnabled notifyEvents notifyTasks notifyLists notifyPeople }
            }
        }
        """;

    // -------------------------------------------------------------------------
    // QUERIES
    // -------------------------------------------------------------------------

    public const string GetTenants = """
        query GetTenants {
            tenants {
                nodes { id name slug }
            }
        }
        """;

    public const string GetUsers = """
        query GetUsers {
            users {
                nodes { id displayName email timezone }
            }
        }
        """;

    public const string GetSpaces = """
        query GetSpaces {
            spaces {
                nodes { id name spaceType }
            }
        }
        """;

    public const string GetSpaceMemberships = """
        query GetSpaceMemberships {
            spaceMemberships {
                nodes { id spaceId userId role }
            }
        }
        """;

    public const string GetProjects = """
        query GetProjects($spaceId: UUID) {
            projects(spaceId: $spaceId) {
                nodes { id name description spaceId }
            }
        }
        """;

    public const string GetTaskItems = """
        query GetTaskItems($spaceId: UUID) {
            taskItems(spaceId: $spaceId) {
                nodes { id title status priority projectId spaceId completedAt }
            }
        }
        """;

    public const string GetCalendars = """
        query GetCalendars($spaceId: UUID) {
            calendars(spaceId: $spaceId) {
                nodes { id name color isDefault spaceId }
            }
        }
        """;

    public const string GetCalendarEvents = """
        query GetCalendarEvents($calendarId: UUID) {
            calendarEvents(calendarId: $calendarId) {
                nodes { id title isAllDay startAt endAt calendarId }
            }
        }
        """;

    public const string GetEventsForRange = """
        query GetEventsForRange($calendarIds: [UUID!]!, $rangeStart: DateTime!, $rangeEnd: DateTime!, $timezone: String!) {
            eventsForRange(calendarIds: $calendarIds, rangeStart: $rangeStart, rangeEnd: $rangeEnd, timezone: $timezone) {
                id title startAt endAt isAllDay
            }
        }
        """;

    public const string GetRecurringOccurrences = """
        query GetRecurringOccurrences($taskItemId: UUID!, $timezone: String!, $rangeStart: DateTime!, $rangeEnd: DateTime!) {
            recurringOccurrences(taskItemId: $taskItemId, timezone: $timezone, rangeStart: $rangeStart, rangeEnd: $rangeEnd) {
                dateTime
            }
        }
        """;

    public const string GetShoppingLists = """
        query GetShoppingLists($spaceId: UUID) {
            shoppingLists(spaceId: $spaceId) {
                nodes { id name spaceId }
            }
        }
        """;

    public const string GetPersons = """
        query GetPersons($spaceId: UUID) {
            persons(spaceId: $spaceId) {
                nodes { id firstName lastName spaceId }
            }
        }
        """;

    public const string GetDevices = """
        query GetDevices($userId: UUID) {
            devices(userId: $userId) {
                nodes { id deviceName platform userId }
            }
        }
        """;

    public const string GetAttachments = """
        query GetAttachments($taskItemId: UUID, $calendarEventId: UUID, $personId: UUID) {
            attachments(taskItemId: $taskItemId, calendarEventId: $calendarEventId, personId: $personId) {
                nodes { id fileName contentType fileSize }
            }
        }
        """;

    public const string GetProjectById = """
        query GetProjectById($id: UUID!) {
            projectById(id: $id) {
                id name description spaceId
            }
        }
        """;

    public const string GetCalendarById = """
        query GetCalendarById($id: UUID!) {
            calendarById(id: $id) {
                id name color spaceId
            }
        }
        """;

    public const string GetTaskItemById = """
        query GetTaskItemById($id: UUID!) {
            taskItemById(id: $id) {
                id title status priority projectId completedAt
            }
        }
        """;
}
