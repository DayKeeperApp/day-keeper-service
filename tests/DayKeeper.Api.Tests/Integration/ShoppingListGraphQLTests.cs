using System.Net;
using System.Net.Http.Json;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class ShoppingListGraphQLTests
{
    private readonly HttpClient _client;

    public ShoppingListGraphQLTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Queries ──────────────────────────────────────────────────────

    [Fact]
    public async Task ShoppingLists_Query_ReturnsConnectionType()
    {
        var query = new
        {
            query = """
                {
                    shoppingLists {
                        edges {
                            cursor
                            node {
                                id
                                name
                                normalizedName
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
        content.Should().Contain("\"shoppingLists\"");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task ShoppingLists_Query_WithSpaceIdFilter_ReturnsFilteredResults()
    {
        var spaceId = await CreateSpaceAsync();
        var listName = $"List-{Guid.NewGuid():N}";
        await CreateShoppingListAsync(spaceId, listName);

        var query = new
        {
            query = $$"""
                {
                    shoppingLists(spaceId: "{{spaceId}}") {
                        edges {
                            node {
                                id
                                name
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
        content.Should().Contain($"\"name\":\"{listName}\"");
    }

    [Fact]
    public async Task ShoppingListById_Query_ReturnsNullForNonExistent()
    {
        var id = Guid.NewGuid();
        var query = new
        {
            query = $$"""
                {
                    shoppingListById(id: "{{id}}") {
                        id
                        name
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"shoppingListById\":null");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task ShoppingListById_Query_WhenExists_ReturnsShoppingList()
    {
        var spaceId = await CreateSpaceAsync();
        var listName = $"List-{Guid.NewGuid():N}";
        var listId = await CreateShoppingListAsync(spaceId, listName);

        var query = new
        {
            query = $$"""
                {
                    shoppingListById(id: "{{listId}}") {
                        id
                        name
                        normalizedName
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain($"\"name\":\"{listName}\"");
        content.Should().NotContain("\"errors\"");
    }

    // ── ShoppingList Create Mutation ─────────────────────────────────

    [Fact]
    public async Task CreateShoppingList_Mutation_ReturnsShoppingList()
    {
        var spaceId = await CreateSpaceAsync();
        var listName = $"List-{Guid.NewGuid():N}";

        var mutation = new
        {
            query = $$"""
                mutation {
                    createShoppingList(input: {
                        spaceId: "{{spaceId}}"
                        name: "{{listName}}"
                    }) {
                        shoppingList {
                            id
                            name
                            normalizedName
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain($"\"name\":\"{listName}\"");
        content.Should().NotContain("EntityNotFoundError");
        content.Should().NotContain("DuplicateShoppingListNameError");
    }

    [Fact]
    public async Task CreateShoppingList_Mutation_DuplicateName_ReturnsError()
    {
        var spaceId = await CreateSpaceAsync();
        var listName = $"Dup-{Guid.NewGuid():N}";
        await CreateShoppingListAsync(spaceId, listName);

        var duplicate = new
        {
            query = $$"""
                mutation {
                    createShoppingList(input: {
                        spaceId: "{{spaceId}}"
                        name: "{{listName}}"
                    }) {
                        shoppingList { id }
                        errors {
                            __typename
                            ... on DuplicateShoppingListNameError {
                                message
                            }
                        }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", duplicate);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("DuplicateShoppingListNameError");
    }

    [Fact]
    public async Task CreateShoppingList_Mutation_NonExistentSpace_ReturnsError()
    {
        var fakeSpaceId = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    createShoppingList(input: {
                        spaceId: "{{fakeSpaceId}}"
                        name: "Groceries"
                    }) {
                        shoppingList { id }
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

    // ── ShoppingList Update Mutation ─────────────────────────────────

    [Fact]
    public async Task UpdateShoppingList_Mutation_ReturnsUpdatedShoppingList()
    {
        var spaceId = await CreateSpaceAsync();
        var listId = await CreateShoppingListAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    updateShoppingList(input: { id: "{{listId}}", name: "Updated List" }) {
                        shoppingList {
                            id
                            name
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"name\":\"Updated List\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task UpdateShoppingList_Mutation_NotFound_ReturnsError()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    updateShoppingList(input: { id: "{{id}}", name: "Nope" }) {
                        shoppingList { id }
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
    public async Task UpdateShoppingList_Mutation_DuplicateName_ReturnsError()
    {
        var spaceId = await CreateSpaceAsync();
        var firstName = $"First-{Guid.NewGuid():N}";
        var secondName = $"Second-{Guid.NewGuid():N}";
        await CreateShoppingListAsync(spaceId, firstName);
        var secondId = await CreateShoppingListAsync(spaceId, secondName);

        var mutation = new
        {
            query = $$"""
                mutation {
                    updateShoppingList(input: { id: "{{secondId}}", name: "{{firstName}}" }) {
                        shoppingList { id }
                        errors {
                            __typename
                            ... on DuplicateShoppingListNameError {
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
        content.Should().Contain("DuplicateShoppingListNameError");
    }

    // ── ShoppingList Delete Mutation ─────────────────────────────────

    [Fact]
    public async Task DeleteShoppingList_Mutation_ReturnsTrue()
    {
        var spaceId = await CreateSpaceAsync();
        var listId = await CreateShoppingListAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteShoppingList(input: { id: "{{listId}}" }) {
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
    public async Task DeleteShoppingList_Mutation_WhenNotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteShoppingList(input: { id: "{{id}}" }) {
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

    // ── ListItem Create Mutation ────────────────────────────────────

    [Fact]
    public async Task CreateListItem_Mutation_ReturnsListItem()
    {
        var spaceId = await CreateSpaceAsync();
        var listId = await CreateShoppingListAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    createListItem(input: {
                        shoppingListId: "{{listId}}"
                        name: "Milk"
                        quantity: 1
                        sortOrder: 0
                    }) {
                        listItem {
                            id
                            name
                            quantity
                            isChecked
                            sortOrder
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"name\":\"Milk\"");
        content.Should().Contain("\"isChecked\":false");
        content.Should().Contain("\"sortOrder\":0");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task CreateListItem_Mutation_WithUnit_ReturnsUnit()
    {
        var spaceId = await CreateSpaceAsync();
        var listId = await CreateShoppingListAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    createListItem(input: {
                        shoppingListId: "{{listId}}"
                        name: "Milk"
                        quantity: 1
                        unit: "gallon"
                        sortOrder: 0
                    }) {
                        listItem {
                            name
                            unit
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"unit\":\"gallon\"");
    }

    [Fact]
    public async Task CreateListItem_Mutation_WithDecimalQuantity_ReturnsCorrectValue()
    {
        var spaceId = await CreateSpaceAsync();
        var listId = await CreateShoppingListAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    createListItem(input: {
                        shoppingListId: "{{listId}}"
                        name: "Cheese"
                        quantity: 1.75
                        sortOrder: 0
                    }) {
                        listItem {
                            name
                            quantity
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("1.75");
    }

    [Fact]
    public async Task CreateListItem_Mutation_NonExistentShoppingList_ReturnsError()
    {
        var fakeListId = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    createListItem(input: {
                        shoppingListId: "{{fakeListId}}"
                        name: "Milk"
                        quantity: 1
                        sortOrder: 0
                    }) {
                        listItem { id }
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

    // ── ListItem Update Mutation ────────────────────────────────────

    [Fact]
    public async Task UpdateListItem_Mutation_ReturnsUpdatedItem()
    {
        var spaceId = await CreateSpaceAsync();
        var listId = await CreateShoppingListAsync(spaceId);
        var itemId = await CreateListItemAsync(listId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    updateListItem(input: { id: "{{itemId}}", name: "Bread" }) {
                        listItem {
                            id
                            name
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"name\":\"Bread\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task UpdateListItem_Mutation_CheckItem_ReturnsCheckedTrue()
    {
        var spaceId = await CreateSpaceAsync();
        var listId = await CreateShoppingListAsync(spaceId);
        var itemId = await CreateListItemAsync(listId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    updateListItem(input: { id: "{{itemId}}", isChecked: true }) {
                        listItem {
                            isChecked
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"isChecked\":true");
    }

    [Fact]
    public async Task UpdateListItem_Mutation_UncheckItem_ReturnsCheckedFalse()
    {
        var spaceId = await CreateSpaceAsync();
        var listId = await CreateShoppingListAsync(spaceId);
        var itemId = await CreateListItemAsync(listId);

        // Check first
        var check = new
        {
            query = $$"""
                mutation {
                    updateListItem(input: { id: "{{itemId}}", isChecked: true }) {
                        listItem { isChecked }
                        errors { __typename }
                    }
                }
                """
        };
        await _client.PostAsJsonAsync("/graphql", check);

        // Uncheck
        var uncheck = new
        {
            query = $$"""
                mutation {
                    updateListItem(input: { id: "{{itemId}}", isChecked: false }) {
                        listItem {
                            isChecked
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", uncheck);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"isChecked\":false");
    }

    [Fact]
    public async Task UpdateListItem_Mutation_UpdateSortOrder_ReturnsNewOrder()
    {
        var spaceId = await CreateSpaceAsync();
        var listId = await CreateShoppingListAsync(spaceId);
        var itemId = await CreateListItemAsync(listId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    updateListItem(input: { id: "{{itemId}}", sortOrder: 5 }) {
                        listItem {
                            sortOrder
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"sortOrder\":5");
    }

    [Fact]
    public async Task UpdateListItem_Mutation_NotFound_ReturnsError()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    updateListItem(input: { id: "{{id}}", name: "Nope" }) {
                        listItem { id }
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

    // ── ListItem Delete Mutation ────────────────────────────────────

    [Fact]
    public async Task DeleteListItem_Mutation_ReturnsTrue()
    {
        var spaceId = await CreateSpaceAsync();
        var listId = await CreateShoppingListAsync(spaceId);
        var itemId = await CreateListItemAsync(listId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteListItem(input: { id: "{{itemId}}" }) {
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
    public async Task DeleteListItem_Mutation_WhenNotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteListItem(input: { id: "{{id}}" }) {
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

    // ── End-to-End Flow ─────────────────────────────────────────────

    [Fact]
    public async Task EndToEnd_CreateListAddItemsCheckAndDelete_Succeeds()
    {
        // 1. Create list
        var spaceId = await CreateSpaceAsync();
        var listId = await CreateShoppingListAsync(spaceId, $"E2E-{Guid.NewGuid():N}");

        // 2. Add items
        var item1Id = await CreateListItemAsync(listId, "Milk", 1, null, 0);
        var item2Id = await CreateListItemAsync(listId, "Bread", 2, "loaves", 1);

        // 3. Check item 1
        var checkMutation = new
        {
            query = $$"""
                mutation {
                    updateListItem(input: { id: "{{item1Id}}", isChecked: true }) {
                        listItem { isChecked }
                        errors { __typename }
                    }
                }
                """
        };
        var checkResponse = await _client.PostAsJsonAsync("/graphql", checkMutation);
        var checkContent = await checkResponse.Content.ReadAsStringAsync();
        checkContent.Should().Contain("\"isChecked\":true");

        // 4. Delete checked item
        var deleteMutation = new
        {
            query = $$"""
                mutation {
                    deleteListItem(input: { id: "{{item1Id}}" }) {
                        boolean
                    }
                }
                """
        };
        var deleteResponse = await _client.PostAsJsonAsync("/graphql", deleteMutation);
        var deleteContent = await deleteResponse.Content.ReadAsStringAsync();
        deleteContent.Should().Contain("true");

        // 5. Delete list
        var deleteListMutation = new
        {
            query = $$"""
                mutation {
                    deleteShoppingList(input: { id: "{{listId}}" }) {
                        boolean
                    }
                }
                """
        };
        var deleteListResponse = await _client.PostAsJsonAsync("/graphql", deleteListMutation);
        var deleteListContent = await deleteListResponse.Content.ReadAsStringAsync();
        deleteListContent.Should().Contain("true");
    }

    // ── Validation Integration ──────────────────────────────────────

    [Fact]
    public async Task CreateShoppingList_EmptyName_ReturnsInputValidationError()
    {
        var spaceId = await CreateSpaceAsync();
        var mutation = new
        {
            query = $$"""
                mutation {
                    createShoppingList(input: {
                        spaceId: "{{spaceId}}"
                        name: ""
                    }) {
                        shoppingList { id }
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

    [Fact]
    public async Task CreateListItem_NegativeQuantity_ReturnsInputValidationError()
    {
        var spaceId = await CreateSpaceAsync();
        var listId = await CreateShoppingListAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    createListItem(input: {
                        shoppingListId: "{{listId}}"
                        name: "Milk"
                        quantity: -1
                        sortOrder: 0
                    }) {
                        listItem { id }
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

    private async Task<string> CreateShoppingListAsync(string spaceId, string? name = null)
    {
        name ??= $"List-{Guid.NewGuid():N}";
        var mutation = new
        {
            query = $$"""
                mutation {
                    createShoppingList(input: {
                        spaceId: "{{spaceId}}"
                        name: "{{name}}"
                    }) {
                        shoppingList { id }
                        errors { __typename }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", mutation).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ExtractId(content);
    }

    private async Task<string> CreateListItemAsync(
        string shoppingListId,
        string name = "Milk",
        decimal quantity = 1,
        string? unit = null,
        int sortOrder = 0)
    {
        var unitField = unit is not null ? $"unit: \"{unit}\"" : "";
        var mutation = new
        {
            query = $$"""
                mutation {
                    createListItem(input: {
                        shoppingListId: "{{shoppingListId}}"
                        name: "{{name}}"
                        quantity: {{quantity}}
                        {{unitField}}
                        sortOrder: {{sortOrder}}
                    }) {
                        listItem { id }
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
