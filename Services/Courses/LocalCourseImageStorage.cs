using Microsoft.AspNetCore.Http;

namespace Online_Tuition_Systems.Services.Courses;

public sealed class LocalCourseImageStorage(IWebHostEnvironment environment)
    : ILocalCourseImageStorage
{
    private const string RelativeDirectory = "uploads/courses";

    public async Task<string?> SaveAsync(
        IFormFile? image,
        CancellationToken cancellationToken)
    {
        if (image is null || image.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var directory = Path.Combine(
            environment.WebRootPath,
            "uploads",
            "courses");

        Directory.CreateDirectory(directory);

        var physicalPath = Path.Combine(directory, fileName);
        await using var stream = new FileStream(
            physicalPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        await image.CopyToAsync(stream, cancellationToken);
        return $"/{RelativeDirectory}/{fileName}";
    }

    public Task DeleteAsync(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Task.CompletedTask;
        }

        var normalizedPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var expectedRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath, RelativeDirectory));
        var physicalPath = Path.GetFullPath(Path.Combine(environment.WebRootPath, normalizedPath));

        var relativeToExpectedRoot = Path.GetRelativePath(expectedRoot, physicalPath);
        if (!relativeToExpectedRoot.StartsWith("..", StringComparison.Ordinal)
            && !Path.IsPathRooted(relativeToExpectedRoot)
            && File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return Task.CompletedTask;
    }
}
