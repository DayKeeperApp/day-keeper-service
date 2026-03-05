using DayKeeper.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class AttachmentStorageServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AttachmentStorageService _sut;

    public AttachmentStorageServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"attachment-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["AttachmentStorage:BasePath"] = _tempDir,
            })
            .Build();

        _sut = new AttachmentStorageService(config);
    }

    // ── SaveAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_WritesFileToCorrectPath()
    {
        var content = "hello world"u8.ToArray();
        await _sut.SaveAsync("tenant/type/parent/att/file.jpg", new MemoryStream(content));

        var fullPath = Path.Combine(_tempDir, "tenant", "type", "parent", "att", "file.jpg");
        File.Exists(fullPath).Should().BeTrue();
        var written = await File.ReadAllBytesAsync(fullPath);
        written.Should().Equal(content);
    }

    [Fact]
    public async Task SaveAsync_CreatesIntermediateDirectories()
    {
        await _sut.SaveAsync("a/b/c/d/e.txt", new MemoryStream([0x01]));

        var fullPath = Path.Combine(_tempDir, "a", "b", "c", "d", "e.txt");
        File.Exists(fullPath).Should().BeTrue();
    }

    // ── ReadAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ReadAsync_WhenFileExists_ReturnsReadableStream()
    {
        var expected = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };
        var filePath = Path.Combine(_tempDir, "read-test.bin");
        await File.WriteAllBytesAsync(filePath, expected);

        var stream = await _sut.ReadAsync("read-test.bin");
        await using (stream.ConfigureAwait(false))
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.ToArray().Should().Equal(expected);
        }
    }

    [Fact]
    public async Task ReadAsync_WhenFileNotExists_ThrowsFileNotFoundException()
    {
        var act = () => _sut.ReadAsync("does-not-exist.bin");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    // ── DeleteAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenFileExists_ReturnsTrueAndRemovesFile()
    {
        var filePath = Path.Combine(_tempDir, "to-delete.bin");
        await File.WriteAllBytesAsync(filePath, [0x01]);

        var result = await _sut.DeleteAsync("to-delete.bin");

        result.Should().BeTrue();
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenFileNotExists_ReturnsFalse()
    {
        var result = await _sut.DeleteAsync("missing.bin");

        result.Should().BeFalse();
    }

    // ── Directory Traversal Prevention ──────────────────────────────

    [Fact]
    public async Task SaveAsync_WithTraversalPath_ThrowsArgumentException()
    {
        var act = () => _sut.SaveAsync("../../etc/passwd", new MemoryStream([0x01]));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ReadAsync_WithTraversalPath_ThrowsArgumentException()
    {
        var act = () => _sut.ReadAsync("../../etc/passwd");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteAsync_WithTraversalPath_ThrowsArgumentException()
    {
        var act = () => _sut.DeleteAsync("../../etc/passwd");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── Constructor ─────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithMissingBasePath_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal))
            .Build();

        var act = () => new AttachmentStorageService(config);

        act.Should().Throw<InvalidOperationException>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
