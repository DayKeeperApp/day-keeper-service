using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DayKeeper.Application.Interfaces;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class AttachmentEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AttachmentEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithMockedStorage(
        IAttachmentStorageService storageService)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAttachmentStorageService>();
                services.AddSingleton(storageService);
            });
        }).CreateClient();
    }

    private static IAttachmentStorageService CreateMockStorage(byte[]? readContent = null)
    {
        var storageService = Substitute.For<IAttachmentStorageService>();
        storageService.SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        storageService.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult<Stream>(new MemoryStream(readContent ?? "test-content"u8.ToArray())));
        storageService.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        return storageService;
    }

    private static MultipartFormDataContent CreateUploadContent(
        byte[] fileBytes,
        string fileName,
        string contentType,
        Guid? calendarEventId = null)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        if (calendarEventId.HasValue)
            content.Add(new StringContent(calendarEventId.Value.ToString()), "calendarEventId");

        return content;
    }

    // ── Upload (POST /api/v1/attachments) ───────────────────────────

    [Fact]
    public async Task Upload_WithValidFile_Returns201WithMetadata()
    {
        var client = CreateClientWithMockedStorage(CreateMockStorage());
        var (tenantId, eventId) = await SeedTenantAndEventAsync(client);

        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        var uploadContent = CreateUploadContent(
            [0x01, 0x02, 0x03, 0x04], "photo.jpg", "image/jpeg",
            calendarEventId: Guid.Parse(eventId));

        var response = await client.PostAsync("/api/v1/attachments", uploadContent);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"fileName\":\"photo.jpg\"");
        body.Should().Contain("\"contentType\":\"image/jpeg\"");
        body.Should().Contain("\"fileSize\":4");
    }

    [Fact]
    public async Task Upload_WithDisallowedContentType_Returns400()
    {
        var client = CreateClientWithMockedStorage(CreateMockStorage());
        var (tenantId, eventId) = await SeedTenantAndEventAsync(client);

        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        var uploadContent = CreateUploadContent(
            [0x01], "file.txt", "text/plain", calendarEventId: Guid.Parse(eventId));

        var response = await client.PostAsync("/api/v1/attachments", uploadContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("contentType");
    }

    [Fact]
    public async Task Upload_WithNoParentId_Returns400()
    {
        var client = CreateClientWithMockedStorage(CreateMockStorage());
        var (tenantId, _) = await SeedTenantAndEventAsync(client);

        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        var uploadContent = CreateUploadContent([0x01], "photo.jpg", "image/jpeg");

        var response = await client.PostAsync("/api/v1/attachments", uploadContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("parentId");
    }

    [Fact]
    public async Task Upload_WithNonExistentParent_Returns404()
    {
        var client = CreateClientWithMockedStorage(CreateMockStorage());
        var (tenantId, _) = await SeedTenantAndEventAsync(client);

        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        var uploadContent = CreateUploadContent(
            [0x01], "photo.jpg", "image/jpeg", calendarEventId: Guid.NewGuid());

        var response = await client.PostAsync("/api/v1/attachments", uploadContent);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Download (GET /api/v1/attachments/{id}) ─────────────────────

    [Fact]
    public async Task Download_WhenExists_Returns200WithFileStream()
    {
        var expectedBytes = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };
        var client = CreateClientWithMockedStorage(CreateMockStorage(readContent: expectedBytes));
        var attachmentId = await UploadAttachmentAsync(client);

        var response = await client.GetAsync($"/api/v1/attachments/{attachmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task Download_WhenNotExists_Returns404()
    {
        var client = CreateClientWithMockedStorage(CreateMockStorage());

        var response = await client.GetAsync($"/api/v1/attachments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Metadata (GET /api/v1/attachments/{id}/metadata) ────────────

    [Fact]
    public async Task GetMetadata_WhenExists_Returns200WithJson()
    {
        var client = CreateClientWithMockedStorage(CreateMockStorage());
        var attachmentId = await UploadAttachmentAsync(client);

        var response = await client.GetAsync($"/api/v1/attachments/{attachmentId}/metadata");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"fileName\":\"photo.jpg\"");
        body.Should().Contain("\"contentType\":\"image/jpeg\"");
    }

    [Fact]
    public async Task GetMetadata_WhenNotExists_Returns404()
    {
        var client = CreateClientWithMockedStorage(CreateMockStorage());

        var response = await client.GetAsync($"/api/v1/attachments/{Guid.NewGuid()}/metadata");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Delete (DELETE /api/v1/attachments/{id}) ────────────────────

    [Fact]
    public async Task Delete_WhenExists_Returns204()
    {
        var client = CreateClientWithMockedStorage(CreateMockStorage());
        var attachmentId = await UploadAttachmentAsync(client);

        var response = await client.DeleteAsync($"/api/v1/attachments/{attachmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WhenNotExists_Returns404()
    {
        var client = CreateClientWithMockedStorage(CreateMockStorage());

        var response = await client.DeleteAsync($"/api/v1/attachments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Seed helpers (ConfigureAwait used only in non-test methods) ──

    private static async Task<string> UploadAttachmentAsync(HttpClient client)
    {
        var (tenantId, eventId) = await SeedTenantAndEventAsync(client).ConfigureAwait(false);

        if (!client.DefaultRequestHeaders.Contains("X-Tenant-Id"))
            client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);

        var uploadContent = CreateUploadContent(
            [0x01, 0x02, 0x03], "photo.jpg", "image/jpeg",
            calendarEventId: Guid.Parse(eventId));

        var response = await client.PostAsync("/api/v1/attachments", uploadContent).ConfigureAwait(false);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ExtractJsonField(body, "id");
    }

    private static async Task<(string TenantId, string EventId)> SeedTenantAndEventAsync(HttpClient client)
    {
        var tenantId = await CreateEntityAsync(client, $$"""
            mutation { createTenant(input: { name: "T", slug: "t-{{Guid.NewGuid():N}}" }) {
                tenant { id } errors { __typename } } }
            """).ConfigureAwait(false);

        var userId = await CreateEntityAsync(client, $$"""
            mutation { createUser(input: {
                tenantId: "{{tenantId}}", displayName: "U",
                email: "u-{{Guid.NewGuid():N}}@example.com", timezone: "UTC", weekStart: SUNDAY
            }) { user { id } errors { __typename } } }
            """).ConfigureAwait(false);

        var spaceId = await CreateEntityAsync(client, $$"""
            mutation { createSpace(input: {
                tenantId: "{{tenantId}}", name: "S-{{Guid.NewGuid():N}}",
                spaceType: PERSONAL, createdByUserId: "{{userId}}"
            }) { space { id } errors { __typename } } }
            """).ConfigureAwait(false);

        var calId = await CreateEntityAsync(client, $$"""
            mutation { createCalendar(input: {
                spaceId: "{{spaceId}}", name: "C-{{Guid.NewGuid():N}}",
                color: "#4A90D9", isDefault: false
            }) { calendar { id } errors { __typename } } }
            """).ConfigureAwait(false);

        var eventId = await CreateEntityAsync(client, $$"""
            mutation { createCalendarEvent(input: {
                calendarId: "{{calId}}", title: "E-{{Guid.NewGuid():N}}",
                isAllDay: false, startAt: "2026-03-05T10:00:00Z",
                endAt: "2026-03-05T11:00:00Z", timezone: "UTC"
            }) { calendarEvent { id } errors { __typename } } }
            """).ConfigureAwait(false);

        return (tenantId, eventId);
    }

    private static async Task<string> CreateEntityAsync(HttpClient client, string graphqlQuery)
    {
        var response = await client.PostAsJsonAsync("/graphql", new { query = graphqlQuery }).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ExtractJsonField(content, "id");
    }

    private static string ExtractJsonField(string json, string field)
    {
        var marker = $"\"{field}\":\"";
        var start = json.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = json.IndexOf('"', start);
        return json[start..end];
    }
}
