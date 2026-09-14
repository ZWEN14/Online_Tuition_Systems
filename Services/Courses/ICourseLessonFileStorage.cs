using Microsoft.AspNetCore.Http;

namespace Online_Tuition_Systems.Services.Courses;

public interface ICourseLessonFileStorage
{
    const long MaxBytes = 50L * 1024 * 1024;

    Task<CourseLessonStoredFile?> SaveAsync(
        IFormFile? file,
        CancellationToken cancellationToken);

    Stream? OpenRead(string storedName);

    Task DeleteAsync(string? storedName);
}

public sealed record CourseLessonStoredFile(
    string OriginalName,
    string StoredName,
    string ContentType,
    long SizeBytes);
