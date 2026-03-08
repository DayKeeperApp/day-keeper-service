using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class SchemaExportTests
{
    private readonly CustomWebApplicationFactory _factory;

    public SchemaExportTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExportGraphQLSchema()
    {
        // Boot the host so all services are registered
        using var _ = _factory.CreateClient();

        var executorResolver = _factory.Services.GetRequiredService<IRequestExecutorResolver>();
        var executor = await executorResolver.GetRequestExecutorAsync();
        var sdl = executor.Schema.ToString();

        var outputPath = Path.Combine(FindSolutionRoot(), "docs", "api", "schema.graphql");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        if (!sdl.EndsWith('\n'))
        {
            sdl += '\n';
        }

        await File.WriteAllTextAsync(outputPath, sdl);

        Assert.True(File.Exists(outputPath), "schema.graphql was not written");
        Assert.False(string.IsNullOrWhiteSpace(sdl), "Schema SDL is empty");
    }

    private static string FindSolutionRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null && !File.Exists(Path.Combine(dir, "DayKeeper.slnx")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        return dir ?? throw new InvalidOperationException(
            "Could not find solution root (DayKeeper.slnx)");
    }
}
