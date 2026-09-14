using Microsoft.AspNetCore.Http;

namespace Online_Tuition_Systems.Services.Courses;

public interface ICourseworkFileStorage
{
    const long MaxBytes = 20L * 1024 * 1024;

    Task<CourseworkStoredFile?> SaveAsync(IFormFile? file, CancellationToken cancellationToken);
    Stream? OpenRead(string storedName);
    Task DeleteAsync(string? storedName);
}

public sealed record CourseworkStoredFile(
    string OriginalName,
    string StoredName,
    string ContentType,
    long SizeBytes);
