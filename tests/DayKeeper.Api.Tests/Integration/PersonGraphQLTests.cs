using System.Net;
using System.Net.Http.Json;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class PersonGraphQLTests
{
    private readonly HttpClient _client;

    public PersonGraphQLTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Person Queries ────────────────────────────────────────────────

    [Fact]
    public async Task Persons_Query_ReturnsConnectionType()
    {
        var query = new
        {
            query = """
                {
                    persons {
                        edges {
                            cursor
                            node {
                                id
                                firstName
                                lastName
                                normalizedFullName
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
        content.Should().Contain("\"persons\"");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task Persons_Query_WithSpaceIdFilter_ReturnsFilteredResults()
    {
        var spaceId = await CreateSpaceAsync();
        var firstName = $"John-{Guid.NewGuid():N}";
        await CreatePersonAsync(spaceId, firstName, "Doe");

        var query = new
        {
            query = $$"""
                {
                    persons(spaceId: "{{spaceId}}") {
                        edges {
                            node {
                                id
                                firstName
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
        content.Should().Contain($"\"firstName\":\"{firstName}\"");
    }

    [Fact]
    public async Task PersonById_Query_ReturnsNullForNonExistent()
    {
        var id = Guid.NewGuid();
        var query = new
        {
            query = $$"""
                {
                    personById(id: "{{id}}") {
                        id
                        firstName
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"personById\":null");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task PersonById_Query_WhenExists_ReturnsPerson()
    {
        var spaceId = await CreateSpaceAsync();
        var firstName = $"Jane-{Guid.NewGuid():N}";
        var personId = await CreatePersonAsync(spaceId, firstName, "Smith");

        var query = new
        {
            query = $$"""
                {
                    personById(id: "{{personId}}") {
                        id
                        firstName
                        lastName
                        normalizedFullName
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain($"\"firstName\":\"{firstName}\"");
        content.Should().NotContain("\"errors\"");
    }

    // ── Person Create Mutation ────────────────────────────────────────

    [Fact]
    public async Task CreatePerson_Mutation_ReturnsPerson()
    {
        var spaceId = await CreateSpaceAsync();
        var firstName = $"Create-{Guid.NewGuid():N}";

        var mutation = new
        {
            query = $$"""
                mutation {
                    createPerson(input: {
                        spaceId: "{{spaceId}}"
                        firstName: "{{firstName}}"
                        lastName: "Test"
                    }) {
                        person {
                            id
                            firstName
                            lastName
                            normalizedFullName
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain($"\"firstName\":\"{firstName}\"");
        content.Should().NotContain("EntityNotFoundError");
        content.Should().NotContain("DuplicatePersonNameError");
    }

    [Fact]
    public async Task CreatePerson_Mutation_DuplicateName_ReturnsError()
    {
        var spaceId = await CreateSpaceAsync();
        var firstName = $"Dup-{Guid.NewGuid():N}";
        await CreatePersonAsync(spaceId, firstName, "Person");

        var duplicate = new
        {
            query = $$"""
                mutation {
                    createPerson(input: {
                        spaceId: "{{spaceId}}"
                        firstName: "{{firstName}}"
                        lastName: "Person"
                    }) {
                        person { id }
                        errors {
                            __typename
                            ... on DuplicatePersonNameError {
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
        content.Should().Contain("DuplicatePersonNameError");
    }

    [Fact]
    public async Task CreatePerson_Mutation_NonExistentSpace_ReturnsError()
    {
        var fakeSpaceId = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    createPerson(input: {
                        spaceId: "{{fakeSpaceId}}"
                        firstName: "John"
                        lastName: "Doe"
                    }) {
                        person { id }
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

    // ── Person Update Mutation ────────────────────────────────────────

    [Fact]
    public async Task UpdatePerson_Mutation_ReturnsUpdatedPerson()
    {
        var spaceId = await CreateSpaceAsync();
        var personId = await CreatePersonAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    updatePerson(input: { id: "{{personId}}", firstName: "Updated" }) {
                        person {
                            id
                            firstName
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"firstName\":\"Updated\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task UpdatePerson_Mutation_NotFound_ReturnsError()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    updatePerson(input: { id: "{{id}}", firstName: "Nope" }) {
                        person { id }
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

    // ── Person Delete Mutation ────────────────────────────────────────

    [Fact]
    public async Task DeletePerson_Mutation_ReturnsTrue()
    {
        var spaceId = await CreateSpaceAsync();
        var personId = await CreatePersonAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    deletePerson(input: { id: "{{personId}}" }) {
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
    public async Task DeletePerson_Mutation_WhenNotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    deletePerson(input: { id: "{{id}}" }) {
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

    // ── ContactMethod Create Mutation ─────────────────────────────────

    [Fact]
    public async Task CreateContactMethod_Mutation_ReturnsContactMethod()
    {
        var spaceId = await CreateSpaceAsync();
        var personId = await CreatePersonAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    createContactMethod(input: {
                        personId: "{{personId}}"
                        type: PHONE
                        value: "555-1234"
                        isPrimary: false
                    }) {
                        contactMethod {
                            id
                            type
                            value
                            isPrimary
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"value\":\"555-1234\"");
        content.Should().Contain("\"type\":\"PHONE\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task CreateContactMethod_Mutation_WithIsPrimary_ReturnsPrimaryTrue()
    {
        var spaceId = await CreateSpaceAsync();
        var personId = await CreatePersonAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    createContactMethod(input: {
                        personId: "{{personId}}"
                        type: EMAIL
                        value: "test@example.com"
                        isPrimary: true
                    }) {
                        contactMethod {
                            isPrimary
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"isPrimary\":true");
    }

    [Fact]
    public async Task CreateContactMethod_Mutation_NonExistentPerson_ReturnsError()
    {
        var fakePersonId = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    createContactMethod(input: {
                        personId: "{{fakePersonId}}"
                        type: PHONE
                        value: "555-1234"
                        isPrimary: false
                    }) {
                        contactMethod { id }
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

    // ── ContactMethod Update Mutation ─────────────────────────────────

    [Fact]
    public async Task UpdateContactMethod_Mutation_ReturnsUpdatedContactMethod()
    {
        var spaceId = await CreateSpaceAsync();
        var personId = await CreatePersonAsync(spaceId);
        var cmId = await CreateContactMethodAsync(personId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    updateContactMethod(input: { id: "{{cmId}}", value: "555-9999" }) {
                        contactMethod {
                            id
                            value
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"value\":\"555-9999\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task UpdateContactMethod_Mutation_NotFound_ReturnsError()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    updateContactMethod(input: { id: "{{id}}", value: "Nope" }) {
                        contactMethod { id }
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

    // ── ContactMethod Delete Mutation ─────────────────────────────────

    [Fact]
    public async Task DeleteContactMethod_Mutation_ReturnsTrue()
    {
        var spaceId = await CreateSpaceAsync();
        var personId = await CreatePersonAsync(spaceId);
        var cmId = await CreateContactMethodAsync(personId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteContactMethod(input: { id: "{{cmId}}" }) {
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
    public async Task DeleteContactMethod_Mutation_WhenNotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteContactMethod(input: { id: "{{id}}" }) {
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

    // ── Address Create Mutation ───────────────────────────────────────

    [Fact]
    public async Task CreateAddress_Mutation_ReturnsAddress()
    {
        var spaceId = await CreateSpaceAsync();
        var personId = await CreatePersonAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    createAddress(input: {
                        personId: "{{personId}}"
                        street1: "123 Main St"
                        city: "Springfield"
                        country: "US"
                        isPrimary: false
                    }) {
                        address {
                            id
                            street1
                            city
                            country
                            isPrimary
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"street1\":\"123 Main St\"");
        content.Should().Contain("\"city\":\"Springfield\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task CreateAddress_Mutation_NonExistentPerson_ReturnsError()
    {
        var fakePersonId = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    createAddress(input: {
                        personId: "{{fakePersonId}}"
                        street1: "123 Main St"
                        city: "Springfield"
                        country: "US"
                        isPrimary: false
                    }) {
                        address { id }
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

    // ── Address Update Mutation ───────────────────────────────────────

    [Fact]
    public async Task UpdateAddress_Mutation_ReturnsUpdatedAddress()
    {
        var spaceId = await CreateSpaceAsync();
        var personId = await CreatePersonAsync(spaceId);
        var addressId = await CreateAddressAsync(personId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    updateAddress(input: { id: "{{addressId}}", city: "Chicago" }) {
                        address {
                            id
                            city
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"city\":\"Chicago\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task UpdateAddress_Mutation_NotFound_ReturnsError()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    updateAddress(input: { id: "{{id}}", city: "Nope" }) {
                        address { id }
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

    // ── Address Delete Mutation ───────────────────────────────────────

    [Fact]
    public async Task DeleteAddress_Mutation_ReturnsTrue()
    {
        var spaceId = await CreateSpaceAsync();
        var personId = await CreatePersonAsync(spaceId);
        var addressId = await CreateAddressAsync(personId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteAddress(input: { id: "{{addressId}}" }) {
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
    public async Task DeleteAddress_Mutation_WhenNotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteAddress(input: { id: "{{id}}" }) {
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

    // ── ImportantDate Create Mutation ─────────────────────────────────

    [Fact]
    public async Task CreateImportantDate_Mutation_ReturnsImportantDate()
    {
        var spaceId = await CreateSpaceAsync();
        var personId = await CreatePersonAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    createImportantDate(input: {
                        personId: "{{personId}}"
                        label: "Birthday"
                        dateValue: "1990-06-15"
                    }) {
                        importantDate {
                            id
                            label
                            date
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"label\":\"Birthday\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task CreateImportantDate_Mutation_NonExistentPerson_ReturnsError()
    {
        var fakePersonId = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    createImportantDate(input: {
                        personId: "{{fakePersonId}}"
                        label: "Birthday"
                        dateValue: "1990-06-15"
                    }) {
                        importantDate { id }
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

    // ── ImportantDate Update Mutation ─────────────────────────────────

    [Fact]
    public async Task UpdateImportantDate_Mutation_ReturnsUpdatedImportantDate()
    {
        var spaceId = await CreateSpaceAsync();
        var personId = await CreatePersonAsync(spaceId);
        var dateId = await CreateImportantDateAsync(personId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    updateImportantDate(input: { id: "{{dateId}}", label: "Anniversary" }) {
                        importantDate {
                            id
                            label
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"label\":\"Anniversary\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task UpdateImportantDate_Mutation_NotFound_ReturnsError()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    updateImportantDate(input: { id: "{{id}}", label: "Nope" }) {
                        importantDate { id }
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

    // ── ImportantDate Delete Mutation ─────────────────────────────────

    [Fact]
    public async Task DeleteImportantDate_Mutation_ReturnsTrue()
    {
        var spaceId = await CreateSpaceAsync();
        var personId = await CreatePersonAsync(spaceId);
        var dateId = await CreateImportantDateAsync(personId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteImportantDate(input: { id: "{{dateId}}" }) {
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
    public async Task DeleteImportantDate_Mutation_WhenNotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteImportantDate(input: { id: "{{id}}" }) {
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
    public async Task EndToEnd_CreatePersonAddSubEntitiesAndDelete_Succeeds()
    {
        // 1. Create person
        var spaceId = await CreateSpaceAsync();
        var personId = await CreatePersonAsync(spaceId, $"E2E-{Guid.NewGuid():N}", "Test");

        // 2. Add contact method
        var cmId = await CreateContactMethodAsync(personId);

        // 3. Add address
        var addressId = await CreateAddressAsync(personId);

        // 4. Add important date
        var dateId = await CreateImportantDateAsync(personId);

        // 5. Verify person query returns data
        var query = new
        {
            query = $$"""
                {
                    personById(id: "{{personId}}") {
                        id
                        firstName
                    }
                }
                """
        };
        var queryResponse = await _client.PostAsJsonAsync("/graphql", query);
        var queryContent = await queryResponse.Content.ReadAsStringAsync();
        queryContent.Should().Contain("\"id\"");
        queryContent.Should().NotContain("\"personById\":null");

        // 6. Delete person
        var deleteMutation = new
        {
            query = $$"""
                mutation {
                    deletePerson(input: { id: "{{personId}}" }) {
                        boolean
                    }
                }
                """
        };
        var deleteResponse = await _client.PostAsJsonAsync("/graphql", deleteMutation);
        var deleteContent = await deleteResponse.Content.ReadAsStringAsync();
        deleteContent.Should().Contain("true");
    }

    // ── Validation Integration ───────────────────────────────────────

    [Fact]
    public async Task CreatePerson_EmptyLastName_ReturnsInputValidationError()
    {
        var spaceId = await CreateSpaceAsync();
        var mutation = new
        {
            query = $$"""
                mutation {
                    createPerson(input: {
                        spaceId: "{{spaceId}}"
                        firstName: "John"
                        lastName: ""
                    }) {
                        person { id }
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

    private async Task<string> CreatePersonAsync(
        string spaceId,
        string? firstName = null,
        string? lastName = null)
    {
        firstName ??= $"Person-{Guid.NewGuid():N}";
        lastName ??= "Test";
        var mutation = new
        {
            query = $$"""
                mutation {
                    createPerson(input: {
                        spaceId: "{{spaceId}}"
                        firstName: "{{firstName}}"
                        lastName: "{{lastName}}"
                    }) {
                        person { id }
                        errors { __typename }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", mutation).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ExtractId(content);
    }

    private async Task<string> CreateContactMethodAsync(string personId)
    {
        var mutation = new
        {
            query = $$"""
                mutation {
                    createContactMethod(input: {
                        personId: "{{personId}}"
                        type: PHONE
                        value: "555-1234"
                        isPrimary: false
                    }) {
                        contactMethod { id }
                        errors { __typename }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", mutation).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ExtractId(content);
    }

    private async Task<string> CreateAddressAsync(string personId)
    {
        var mutation = new
        {
            query = $$"""
                mutation {
                    createAddress(input: {
                        personId: "{{personId}}"
                        street1: "123 Main St"
                        city: "Springfield"
                        country: "US"
                        isPrimary: false
                    }) {
                        address { id }
                        errors { __typename }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", mutation).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ExtractId(content);
    }

    private async Task<string> CreateImportantDateAsync(string personId)
    {
        var mutation = new
        {
            query = $$"""
                mutation {
                    createImportantDate(input: {
                        personId: "{{personId}}"
                        label: "Birthday"
                        dateValue: "1990-06-15"
                    }) {
                        importantDate { id }
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
