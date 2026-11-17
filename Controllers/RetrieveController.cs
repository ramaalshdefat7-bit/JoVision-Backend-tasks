
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using JoVisionBackendTasks;
using System.Text.Json;

namespace JoVisionBackendTasks.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RetrieveController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public RetrieveController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Retrieve(
            [FromQuery] string? FileName,
            [FromQuery] string? FileOwner)
        {
            // Validate the input parameters.
            if (string.IsNullOrWhiteSpace(FileName) ||
                string.IsNullOrWhiteSpace(FileOwner))
            {
                return BadRequest("FileName and FileOwner are required.");
            }

            // Prevent directory traversal and allow JPG files only.
            string fileName = Path.GetFileName(FileName);

            if (!string.Equals(fileName, FileName, StringComparison.Ordinal) ||
                !string.Equals(
                    Path.GetExtension(fileName),
                    ".jpg",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid JPG file name.");
            }

            string folderPath = Path.Combine(
                _environment.ContentRootPath,
                "UploadedImages");

            string imagePath = Path.Combine(folderPath, fileName);
            string metadataPath = imagePath + ".json";

            // Return NotFound if the image or metadata does not exist.
            if (!System.IO.File.Exists(imagePath) ||
                !System.IO.File.Exists(metadataPath))
            {
                return NotFound("Image not found.");
            }

            try
            {
                // Read the image metadata.
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

                // Verify file ownership.
                if (!string.Equals(
                    metadata.Owner.Trim(),
                    FileOwner.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        "Forbidden: You are not the owner of this file.");
                }

                // Return the actual JPG image.
                return PhysicalFile(
                    imagePath,
                    "image/jpeg",
                    fileName);
            }
            catch (JsonException)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Invalid image metadata.");
            }
            catch (Exception)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Internal server error.");
            }
        }
    }
}
