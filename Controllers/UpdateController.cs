
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using JoVisionBackendTasks;
using System.Text.Json;

namespace JoVisionBackendTasks.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UpdateController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public UpdateController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(
    IFormFile? image,
    [FromForm(Name = "Owner")] string? owner,
    [FromForm(Name = "FileName")] string? targetFileName)
        {
            // Validate the uploaded image and owner.
            if (image == null || image.Length == 0 ||
                string.IsNullOrWhiteSpace(owner))
            {
                return BadRequest("Image and Owner are required.");
            }

            // Check the file extension.
            string fileName = Path.GetFileName(targetFileName ?? "");
            if (string.IsNullOrWhiteSpace(targetFileName) ||
    !string.Equals(
        Path.GetExtension(fileName),
        ".jpg",
        StringComparison.OrdinalIgnoreCase) ||
    !string.Equals(
        Path.GetExtension(image.FileName),
        ".jpg",
        StringComparison.OrdinalIgnoreCase) ||
    Path.GetFileName(targetFileName) != targetFileName)
            {
                return BadRequest("Invalid JPG file name.");
            }
            

            // Verify JPEG signature.
            await using var inputStream = image.OpenReadStream();

            byte[] signature = new byte[3];

            int bytesRead = await inputStream.ReadAsync(
                signature, 0, signature.Length);

            if (bytesRead != 3 ||
                signature[0] != 0xFF ||
                signature[1] != 0xD8 ||
                signature[2] != 0xFF)
            {
                return BadRequest("Invalid JPG image.");
            }

            inputStream.Position = 0;

            string folderPath = Path.Combine(
                _environment.ContentRootPath,
                "UploadedImages");

            string imagePath = Path.Combine(folderPath, fileName);
            string metadataPath = imagePath + ".json";

            // Update is only allowed for an existing image.
            if (!System.IO.File.Exists(imagePath) ||
                !System.IO.File.Exists(metadataPath))
            {
                return BadRequest("Image does not exist.");
            }

            string tempPath = Path.Combine(
                folderPath,
                Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                // Read existing metadata.
                string json = await System.IO.File.ReadAllTextAsync(
                    metadataPath);

                ImageMetadata? metadata =
                    JsonSerializer.Deserialize<ImageMetadata>(json);

                if (metadata == null ||
                    string.IsNullOrWhiteSpace(metadata.Owner))
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        "Invalid image metadata.");
                }

                // Check ownership.
                if (!string.Equals(
                    metadata.Owner.Trim(),
                    owner.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        "Forbidden: You are not the owner of this file.");
                }

                // Save the new image to a temporary file first.
                await using (var tempStream = new FileStream(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await inputStream.CopyToAsync(tempStream);
                }

                // Replace the old image.
                System.IO.File.Move(
                    tempPath,
                    imagePath,
                    overwrite: true);

                // Update only LastModifiedAt.
                metadata.LastModifiedAt = DateTime.UtcNow;

                await System.IO.File.WriteAllTextAsync(
                    metadataPath,
                    JsonSerializer.Serialize(
                        metadata,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        }));

                return Ok(new
                {
                    message = "Image updated successfully.",
                    fileName = fileName,
                    metadata = metadata
                });
            }
            catch (Exception)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Internal server error.");
            }
            finally
            {
                // Remove any remaining temporary file.
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }
    }
}
