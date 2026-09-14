using Microsoft.AspNetCore.Http;

namespace Online_Tuition_Systems.Services.Courses;

public sealed class CourseLessonFileStorage(IWebHostEnvironment environment)
    : ICourseLessonFileStorage
{
    private static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".txt"] = "text/plain",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".mp4"] = "video/mp4"
        };

    private string StorageDirectory => Path.Combine(
        environment.ContentRootPath,
        "App_Data",
        "CourseLessonFiles");

    public async Task<CourseLessonStoredFile?> SaveAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > ICourseLessonFileStorage.MaxBytes)
        {
            throw new InvalidDataException("The attachment must be 50 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedTypes.TryGetValue(extension, out var contentType))
        {
            throw new InvalidDataException(
                "Choose a PDF, PPTX, DOCX, XLSX, TXT, JPG, PNG, or MP4 file.");
        }

        await ValidateSignatureAsync(file, extension, cancellationToken);

        Directory.CreateDirectory(StorageDirectory);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = ResolvePath(storedName);
        await using (var destination = new FileStream(
            physicalPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true))
        {
            await file.CopyToAsync(destination, cancellationToken);
        }

        var originalName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (originalName.Length > 255)
        {
            originalName = originalName[^255..];
        }

        return new CourseLessonStoredFile(
            originalName,
            storedName,
            contentType,
            file.Length);
    }

    public Stream? OpenRead(string storedName)
    {
        var physicalPath = ResolvePath(storedName);
        return File.Exists(physicalPath)
            ? new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;
    }

    public Task DeleteAsync(string? storedName)
    {
        if (!string.IsNullOrWhiteSpace(storedName))
        {
            var physicalPath = ResolvePath(storedName);
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storedName)
    {
        var safeName = Path.GetFileName(storedName);
        if (!string.Equals(safeName, storedName, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Invalid lesson attachment path.");
        }

        return Path.Combine(StorageDirectory, safeName);
    }

    private static async Task ValidateSignatureAsync(
        IFormFile file,
        string extension,
        CancellationToken cancellationToken)
    {
        var header = new byte[16];
        await using var source = file.OpenReadStream();
        var count = await source.ReadAsync(header, cancellationToken);
        var bytes = header.AsSpan(0, count);

        var valid = extension switch
        {
            ".pdf" => bytes.StartsWith("%PDF-"u8),
            ".jpg" or ".jpeg" => bytes.StartsWith(new byte[] { 255, 216, 255 }),
            ".png" => bytes.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".pptx" or ".docx" or ".xlsx" => bytes.StartsWith("PK"u8),
            ".mp4" => bytes.Length >= 8 && bytes[4..8].SequenceEqual("ftyp"u8),
            ".txt" => !bytes.Contains((byte)0),
            _ => false
        };

        if (!valid)
        {
            throw new InvalidDataException("The attachment contents do not match its file extension.");
        }
    }
}
