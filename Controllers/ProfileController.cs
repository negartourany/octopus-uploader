using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;

namespace octopus_uploader.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
    private const string ContainerName = "user-images";

    private readonly BlobServiceClient _blobServiceClient;

    private static readonly Dictionary<string, byte[]> ImageSignatures = new()
    {
        { "jpg",  new byte[] { 0xFF, 0xD8, 0xFF } },
        { "jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
        { "png",  new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
        { "gif",  new byte[] { 0x47, 0x49, 0x46, 0x38 } }
    };

    // Constructor: Injects BlobServiceClient from Program.cs
    public ProfileController(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    [HttpPost("photo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto(IFormFile file)
    {
        // 1. Guard against null or empty files
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "No file uploaded." });
        }

        // 2. File size limit (5MB)
        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { success = false, message = "File size exceeds 5MB limit." });
        }

        // 3. Magic bytes validation
        if (!IsValidImageSignature(file))
        {
            return BadRequest(new { success = false, message = "Invalid image file format." });
        }

        // 4. Generate safe UUID filename
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var safeFileName = $"{Guid.NewGuid()}{extension}";

        // 5. Connect to container & upload
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        // Import the models at the top if needed:
        // using Azure.Storage.Blobs.Models;

        await containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
        await containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

        var blobClient = containerClient.GetBlobClient(safeFileName);

        using (var stream = file.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        // 6. Return identical JSON response to Octopus
        var pictureUrl = $"https://id.octopus.com/i/{ContainerName}/{safeFileName}";
        return Ok(new { success = true, pictureUrl });
    }

    // Helper: Validates header magic bytes
    private bool IsValidImageSignature(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var reader = new BinaryReader(stream);

        var headerBytes = reader.ReadBytes(4);

        return ImageSignatures.Values.Any(signature =>
            headerBytes.Take(signature.Length).SequenceEqual(signature));
    }
}