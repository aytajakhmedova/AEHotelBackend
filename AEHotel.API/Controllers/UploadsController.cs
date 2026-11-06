using AEHotel.Service.DTOs.Site;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEHotel.API.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Staff)]
[Route("api/uploads")]
public class UploadsController : ControllerBase
{
    // Only allow safe image formats - SVG excluded due to XSS risks
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".jfif", ".webp", ".gif"
    };

    // Magic bytes (file signatures) for validating actual file content
    private static readonly Dictionary<string, byte[][]> FileSignatures = new()
    {
        { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jfif", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } } }, // GIF87a or GIF89a
        { ".webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } } // RIFF
    };

    private const long MaxFileSize = 10_000_000; // 10 MB

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(IWebHostEnvironment environment, ILogger<UploadsController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<UploadResponse>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationException("A file is required.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new ValidationException($"Fayl ölçüsü {MaxFileSize / 1_000_000} MB-dan çox ola bilməz.");
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new ValidationException("Yalnız PNG, JPG, JPEG, JFIF, GIF və WebP formatları dəstəklənir.");
        }

        // Validate file content by checking magic bytes
        if (!await IsValidImageFileAsync(file, extension, cancellationToken))
        {
            _logger.LogWarning("Invalid file upload attempt: extension={Extension}, name={FileName}", extension, file.FileName);
            throw new ValidationException("Yüklənən fayl şəkil deyil və ya zərərlidir.");
        }

        var folder = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(folder, fileName);
        
        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return Ok(new UploadResponse
        {
            Url = $"/uploads/{fileName}",
            FileName = fileName
        });
    }

    private static async Task<bool> IsValidImageFileAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        if (!FileSignatures.TryGetValue(extension, out var signatures))
        {
            return false;
        }

        await using var stream = file.OpenReadStream();
        var maxSignatureLength = signatures.Max(s => s.Length);
        var headerBytes = new byte[maxSignatureLength];
        var bytesRead = await stream.ReadAsync(headerBytes, cancellationToken);

        if (bytesRead < maxSignatureLength)
        {
            return false;
        }

        // Check if file starts with any of the valid signatures for this extension
        return signatures.Any(signature =>
        {
            for (int i = 0; i < signature.Length; i++)
            {
                if (headerBytes[i] != signature[i])
                {
                    return false;
                }
            }
            return true;
        });
    }
}
