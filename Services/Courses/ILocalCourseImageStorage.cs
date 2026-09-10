using Microsoft.AspNetCore.Http;

namespace Online_Tuition_Systems.Services.Courses;

public interface ILocalCourseImageStorage
{
    Task<string?> SaveAsync(IFormFile? image, CancellationToken cancellationToken);

    Task DeleteAsync(string? relativePath);
}
