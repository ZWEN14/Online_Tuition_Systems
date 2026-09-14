using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.Services;

public class SubmissionUploadService
{
    public const int MaxFiles = 5;
    public const int MaxBytes = 5 * 1024 * 1024;

    // Shared by both modules. Complaints allow photos; surveys also allow PDF/TXT.
    public async Task<List<SubmissionAttachment>> ReadAsync(List<IFormFile> files, bool photosOnly, CancellationToken cancellationToken)
    {
        if (files.Count > MaxFiles)
            throw new InvalidDataException("Choose at most 5 files.");
        var result = new List<SubmissionAttachment>();
        foreach (var file in files)
        {
            if (file.Length is <= 0 or > MaxBytes)
                throw new InvalidDataException("Each file must be nonempty and no larger than 5 MB.");
            await using var source = file.OpenReadStream();
            using var buffer = new MemoryStream();
            // Enforce the size while reading too, not just from the reported length.
            var chunk = new byte[81920];
            int count;
            while ((count = await source.ReadAsync(chunk, cancellationToken)) > 0)
            {
                if (buffer.Length + count > MaxBytes)
                    throw new InvalidDataException("Each file must be no larger than 5 MB.");
                await buffer.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
            }
            var bytes = buffer.ToArray();
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            // Check basic file signatures rather than trusting the browser MIME type.
            string? contentType = extension switch
            {
                ".jpg" or ".jpeg" when bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 }) => "image/jpeg",
                ".png" when bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) => "image/png",
                ".webp" when bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8) => "image/webp",
                ".pdf" when !photosOnly && bytes.AsSpan().StartsWith("%PDF-"u8) => "application/pdf",
                ".txt" when !photosOnly && !bytes.Contains((byte)0) => "text/plain",
                _ => null
            };
            if (contentType is null)
                throw new InvalidDataException(photosOnly
                ? "Choose JPG, PNG, or WebP photos with matching file contents."
                : "Choose JPG, PNG, WebP, PDF, or TXT files with matching file contents.");
            var name = Path.GetFileName(file.FileName.Replace('\\', '/'));
            if (name.Length > 255)
                name = name[^255..];
            result.Add(new SubmissionAttachment { FileName = name, ContentType = contentType, Content = bytes });
        }
        return result;
    }
}
