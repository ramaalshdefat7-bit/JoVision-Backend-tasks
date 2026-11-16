using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using JoVisionBackendTasks;
using System.Text.Json;

namespace JoVisionBackendTasks.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeleteController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public DeleteController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Delete(
            [FromQuery(Name = "FileName")] string? fileName,
            [FromQuery(Name = "FileOwner")] string? fileOwner)
        {
            // Validate the input parameters.
            if (string.IsNullOrWhiteSpace(fileName) ||
                string.IsNullOrWhiteSpace(fileOwner))
            {
                return BadRequest("FileName and FileOwner are required.");
            }

            // Prevent directory traversal and allow JPG files only.
            if (Path.GetFileName(fileName) != fileName ||
                !string.Equals(
                    Path.GetExtension(fileName),
                    ".jpg",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid file name.");
            }

            string folderPath = Path.Combine(
                _environment.ContentRootPath,
                "UploadedImages");

            string imagePath = Path.Combine(folderPath, fileName);
            string metadataPath = imagePath + ".json";

            // Check whether both files exist.
            if (!System.IO.File.Exists(imagePath) ||
                !System.IO.File.Exists(metadataPath))
            {
                return BadRequest("File does not exist.");
            }

            try
            {
                ImageMetadata? metadata;

                // Read the metadata JSON file.
                await using (var jsonStream = new FileStream(
                    metadataPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                {
                    metadata = await JsonSerializer
                        .DeserializeAsync<ImageMetadata>(jsonStream);
                }

                if (metadata == null)
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        "Invalid metadata file.");
                }

                // Check whether the owner matches.
                if (!string.Equals(
                    metadata.Owner,
                    fileOwner.Trim(),
                    StringComparison.Ordinal))
                {
                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        "Forbidden: You are not the owner of this file.");
                }

                // Delete the image and its metadata.
                System.IO.File.Delete(imagePath);
                System.IO.File.Delete(metadataPath);

                return Ok("Deleted");
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