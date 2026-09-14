using Microsoft.AspNetCore.Http;

namespace Online_Tuition_Systems.Services.Courses;

public sealed class CourseworkFileStorage(IWebHostEnvironment environment)
    : ICourseworkFileStorage
{
    private static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".txt"] = "text/plain",
            [".zip"] = "application/zip",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

    private string StorageDirectory => Path.Combine(
        environment.ContentRootPath,
        "App_Data",
        "CourseworkFiles");

    public async Task<CourseworkStoredFile?> SaveAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > ICourseworkFileStorage.MaxBytes)
        {
            throw new InvalidDataException("The attachment must be 20 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedTypes.TryGetValue(extension, out var contentType))
        {
            throw new InvalidDataException(
                "Choose a PDF, PPTX, DOCX, XLSX, TXT, ZIP, JPG, or PNG file.");
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

        return new CourseworkStoredFile(originalName, storedName, contentType, file.Length);
    }

    public Stream? OpenRead(string storedName)
    {
        var path = ResolvePath(storedName);
        return File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;
    }

    public Task DeleteAsync(string? storedName)
    {
        if (!string.IsNullOrWhiteSpace(storedName))
        {
            var path = ResolvePath(storedName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storedName)
    {
        var safeName = Path.GetFileName(storedName);
        if (!string.Equals(safeName, storedName, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Invalid Coursework attachment path.");
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
            ".pptx" or ".docx" or ".xlsx" or ".zip" => bytes.StartsWith("PK"u8),
            ".txt" => !bytes.Contains((byte)0),
            _ => false
        };

        if (!valid)
        {
            throw new InvalidDataException("The attachment contents do not match its file extension.");
        }
    }
}
