using System.Net;
using System.Net.Http.Json;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class TaskItemGraphQLTests
{
    private readonly HttpClient _client;

    public TaskItemGraphQLTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Queries ──────────────────────────────────────────────────────

    [Fact]
    public async Task TaskItems_Query_ReturnsConnectionType()
    {
        var query = new
        {
            query = """
                {
                    taskItems {
                        edges {
                            cursor
                            node {
                                id
                                title
                                status
                                priority
                            }
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"taskItems\"");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task TaskItems_Query_WithSpaceIdFilter_ReturnsFilteredResults()
    {
        var spaceId = await CreateSpaceAsync();
        var taskTitle = $"Task-{Guid.NewGuid():N}";
        await CreateTaskItemAsync(spaceId, taskTitle);

        var query = new
        {
            query = $$"""
                {
                    taskItems(spaceId: "{{spaceId}}") {
                        edges {
                            node {
                                id
                                title
                            }
                        }
                        totalCount
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain($"\"title\":\"{taskTitle}\"");
    }

    [Fact]
    public async Task TaskItemById_Query_ReturnsNullForNonExistent()
    {
        var id = Guid.NewGuid();
        var query = new
        {
            query = $$"""
                {
                    taskItemById(id: "{{id}}") {
                        id
                        title
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"taskItemById\":null");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task TaskItemById_Query_WhenExists_ReturnsTaskItem()
    {
        var spaceId = await CreateSpaceAsync();
        var taskTitle = $"Task-{Guid.NewGuid():N}";
        var taskId = await CreateTaskItemAsync(spaceId, taskTitle);

        var query = new
        {
            query = $$"""
                {
                    taskItemById(id: "{{taskId}}") {
                        id
                        title
                        status
                        priority
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain($"\"title\":\"{taskTitle}\"");
        content.Should().Contain("\"status\":\"OPEN\"");
        content.Should().Contain("\"priority\":\"MEDIUM\"");
        content.Should().NotContain("\"errors\"");
    }

    // ── CreateTaskItem Mutation ───────────────────────────────────────

    [Fact]
    public async Task CreateTaskItem_Mutation_ReturnsTaskItem()
    {
        var spaceId = await CreateSpaceAsync();
        var title = $"Task-{Guid.NewGuid():N}";

        var mutation = new
        {
            query = $$"""
                mutation {
                    createTaskItem(input: {
                        spaceId: "{{spaceId}}"
                        title: "{{title}}"
                        status: OPEN
                        priority: HIGH
                    }) {
                        taskItem {
                            id
                            title
                            status
                            priority
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain($"\"title\":\"{title}\"");
        content.Should().Contain("\"status\":\"OPEN\"");
        content.Should().Contain("\"priority\":\"HIGH\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task CreateTaskItem_Mutation_WithAllFields_ReturnsTaskItem()
    {
        var spaceId = await CreateSpaceAsync();
        var projectId = await CreateProjectAsync(spaceId);
        var title = $"Full-{Guid.NewGuid():N}";

        var mutation = new
        {
            query = $$"""
                mutation {
                    createTaskItem(input: {
                        spaceId: "{{spaceId}}"
                        title: "{{title}}"
                        description: "Test description"
                        projectId: "{{projectId}}"
                        status: IN_PROGRESS
                        priority: URGENT
                        dueAt: "2026-04-01T09:00:00Z"
                        dueDate: "2026-04-01"
                        recurrenceRule: "FREQ=DAILY;COUNT=5"
                    }) {
                        taskItem {
                            id
                            title
                            description
                            status
                            priority
                            recurrenceRule
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain($"\"title\":\"{title}\"");
        content.Should().Contain("\"description\":\"Test description\"");
        content.Should().Contain("\"status\":\"IN_PROGRESS\"");
        content.Should().Contain("\"priority\":\"URGENT\"");
    }

    [Fact]
    public async Task CreateTaskItem_Mutation_NonExistentSpace_ReturnsError()
    {
        var fakeSpaceId = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    createTaskItem(input: {
                        spaceId: "{{fakeSpaceId}}"
                        title: "Task"
                        status: OPEN
                        priority: NONE
                    }) {
                        taskItem { id }
                        errors {
                            __typename
                            ... on EntityNotFoundError {
                                message
                            }
                        }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("EntityNotFoundError");
    }

    [Fact]
    public async Task CreateTaskItem_Mutation_ProjectInWrongSpace_ReturnsError()
    {
        var spaceA = await CreateSpaceAsync();
        var spaceB = await CreateSpaceAsync();
        var projectInB = await CreateProjectAsync(spaceB);

        var mutation = new
        {
            query = $$"""
                mutation {
                    createTaskItem(input: {
                        spaceId: "{{spaceA}}"
                        title: "Task"
                        projectId: "{{projectInB}}"
                        status: OPEN
                        priority: NONE
                    }) {
                        taskItem { id }
                        errors {
                            __typename
                            ... on BusinessRuleViolationError {
                                message
                            }
                        }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("BusinessRuleViolationError");
    }

    [Fact]
    public async Task CreateTaskItem_Mutation_EmptyTitle_ReturnsInputValidationError()
    {
        var spaceId = await CreateSpaceAsync();
        var mutation = new
        {
            query = $$"""
                mutation {
                    createTaskItem(input: {
                        spaceId: "{{spaceId}}"
                        title: ""
                        status: OPEN
                        priority: NONE
                    }) {
                        taskItem { id }
                        errors {
                            __typename
                            ... on InputValidationError {
                                message
                            }
                        }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("InputValidationError");
    }

    // ── UpdateTaskItem Mutation ───────────────────────────────────────

    [Fact]
    public async Task UpdateTaskItem_Mutation_ReturnsUpdatedTaskItem()
    {
        var spaceId = await CreateSpaceAsync();
        var taskId = await CreateTaskItemAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    updateTaskItem(input: {
                        id: "{{taskId}}"
                        title: "Updated Title"
                        priority: LOW
                    }) {
                        taskItem {
                            id
                            title
                            priority
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"title\":\"Updated Title\"");
        content.Should().Contain("\"priority\":\"LOW\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task UpdateTaskItem_Mutation_NotFound_ReturnsError()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    updateTaskItem(input: { id: "{{id}}", title: "Nope" }) {
                        taskItem { id }
                        errors {
                            __typename
                            ... on EntityNotFoundError {
                                message
                            }
                        }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("EntityNotFoundError");
    }

    // ── CompleteTaskItem Mutation ─────────────────────────────────────

    [Fact]
    public async Task CompleteTaskItem_Mutation_SetsStatusToCompleted()
    {
        var spaceId = await CreateSpaceAsync();
        var taskId = await CreateTaskItemAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    completeTaskItem(input: { id: "{{taskId}}" }) {
                        taskItem {
                            status
                            completedAt
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"status\":\"COMPLETED\"");
        content.Should().Contain("\"completedAt\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task CompleteTaskItem_Mutation_NotFound_ReturnsError()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    completeTaskItem(input: { id: "{{id}}" }) {
                        taskItem { id }
                        errors {
                            __typename
                            ... on EntityNotFoundError {
                                message
                            }
                        }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("EntityNotFoundError");
    }

    // ── DeleteTaskItem Mutation ───────────────────────────────────────

    [Fact]
    public async Task DeleteTaskItem_Mutation_ReturnsTrue()
    {
        var spaceId = await CreateSpaceAsync();
        var taskId = await CreateTaskItemAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteTaskItem(input: { id: "{{taskId}}" }) {
                        boolean
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("true");
    }

    [Fact]
    public async Task DeleteTaskItem_Mutation_WhenNotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteTaskItem(input: { id: "{{id}}" }) {
                        boolean
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("false");
    }

    // ── End-to-End Flow ──────────────────────────────────────────────

    [Fact]
    public async Task EndToEnd_CreateUpdateCompleteDeleteTask_Succeeds()
    {
        // 1. Create task
        var spaceId = await CreateSpaceAsync();
        var taskId = await CreateTaskItemAsync(spaceId, $"E2E-{Guid.NewGuid():N}");

        // 2. Update task
        var updateMutation = new
        {
            query = $$"""
                mutation {
                    updateTaskItem(input: {
                        id: "{{taskId}}"
                        title: "Updated E2E"
                        priority: HIGH
                    }) {
                        taskItem { title priority }
                        errors { __typename }
                    }
                }
                """
        };
        var updateResponse = await _client.PostAsJsonAsync("/graphql", updateMutation);
        var updateContent = await updateResponse.Content.ReadAsStringAsync();
        updateContent.Should().Contain("\"title\":\"Updated E2E\"");
        updateContent.Should().Contain("\"priority\":\"HIGH\"");

        // 3. Complete task
        var completeMutation = new
        {
            query = $$"""
                mutation {
                    completeTaskItem(input: { id: "{{taskId}}" }) {
                        taskItem { status completedAt }
                        errors { __typename }
                    }
                }
                """
        };
        var completeResponse = await _client.PostAsJsonAsync("/graphql", completeMutation);
        var completeContent = await completeResponse.Content.ReadAsStringAsync();
        completeContent.Should().Contain("\"status\":\"COMPLETED\"");

        // 4. Delete task
        var deleteMutation = new
        {
            query = $$"""
                mutation {
                    deleteTaskItem(input: { id: "{{taskId}}" }) {
                        boolean
                    }
                }
                """
        };
        var deleteResponse = await _client.PostAsJsonAsync("/graphql", deleteMutation);
        var deleteContent = await deleteResponse.Content.ReadAsStringAsync();
        deleteContent.Should().Contain("true");
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private async Task<string> CreateSpaceAsync()
    {
        var (tenantId, userId) = await CreateTenantAndUserAsync().ConfigureAwait(false);
        var name = $"Space-{Guid.NewGuid():N}";
        var mutation = new
        {
            query = $$"""
                mutation {
                    createSpace(input: {
                        tenantId: "{{tenantId}}"
                        name: "{{name}}"
                        spaceType: PERSONAL
                        createdByUserId: "{{userId}}"
                    }) {
                        space { id }
                        errors { __typename }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", mutation).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ExtractId(content);
    }

    private async Task<string> CreateTaskItemAsync(string spaceId, string? title = null)
    {
        title ??= $"Task-{Guid.NewGuid():N}";
        var mutation = new
        {
            query = $$"""
                mutation {
                    createTaskItem(input: {
                        spaceId: "{{spaceId}}"
                        title: "{{title}}"
                        status: OPEN
                        priority: MEDIUM
                    }) {
                        taskItem { id }
                        errors { __typename }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", mutation).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ExtractId(content);
    }

    private async Task<string> CreateProjectAsync(string spaceId, string? name = null)
    {
        name ??= $"Project-{Guid.NewGuid():N}";
        var mutation = new
        {
            query = $$"""
                mutation {
                    createProject(input: {
                        spaceId: "{{spaceId}}"
                        name: "{{name}}"
                    }) {
                        project { id }
                        errors { __typename }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", mutation).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ExtractId(content);
    }

    private async Task<(string TenantId, string UserId)> CreateTenantAndUserAsync()
    {
        var slug = $"t-{Guid.NewGuid():N}";
        var tenantMutation = new
        {
            query = $$"""
                mutation {
                    createTenant(input: { name: "Tenant", slug: "{{slug}}" }) {
                        tenant { id }
                        errors { __typename }
                    }
                }
                """
        };
        var tenantResponse = await _client.PostAsJsonAsync("/graphql", tenantMutation).ConfigureAwait(false);
        var tenantContent = await tenantResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        var tenantId = ExtractId(tenantContent);

        var email = $"u-{Guid.NewGuid():N}@example.com";
        var userMutation = new
        {
            query = $$"""
                mutation {
                    createUser(input: {
                        tenantId: "{{tenantId}}"
                        displayName: "User"
                        email: "{{email}}"
                        timezone: "UTC"
                        weekStart: SUNDAY
                    }) {
                        user { id }
                        errors { __typename }
                    }
                }
                """
        };
        var userResponse = await _client.PostAsJsonAsync("/graphql", userMutation).ConfigureAwait(false);
        var userContent = await userResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        var userId = ExtractId(userContent);

        return (tenantId, userId);
    }

    private static string ExtractId(string json)
    {
        var marker = "\"id\":\"";
        var start = json.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = json.IndexOf('"', start);
        return json[start..end];
    }
}
