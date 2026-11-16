using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using JoVisionBackendTasks;
using System.Text.Json;

namespace JoVisionBackendTasks.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CreateController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public CreateController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create(
        IFormFile? image,
        [FromForm(Name = "Owner")] string? owner)
        {
            // Validate the uploaded image and owner.
            if (image == null || image.Length == 0 ||
                string.IsNullOrWhiteSpace(owner))
            {
                return BadRequest("Image and Owner are required.");
            }

            // Check the file extension.
            string fileName = Path.GetFileName(image.FileName);

            if (!string.Equals(
                Path.GetExtension(fileName),
                ".jpg",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only JPG images are allowed.");
            }

            // Verify that the file contains JPEG data.
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

            // Create the storage folder.
            string folderPath = Path.Combine(
                _environment.ContentRootPath,
                "UploadedImages");

            Directory.CreateDirectory(folderPath);

            // Create image and metadata paths.
            string imagePath = Path.Combine(folderPath, fileName);
            string metadataPath = imagePath + ".json";

            // Prevent overwriting existing files.
            if (System.IO.File.Exists(imagePath) ||
                System.IO.File.Exists(metadataPath))
            {
                return BadRequest("A file with this name already exists.");
            }

            bool imageCreated = false;
            bool metadataCreated = false;

            try
            {
                // Save the image.
                await using (var fileStream = new FileStream(
                    imagePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    imageCreated = true;
                    await inputStream.CopyToAsync(fileStream);
                }

                // Create image metadata.
                DateTime creationTime = DateTime.UtcNow;

                var metadata = new ImageMetadata
                {
                    Owner = owner.Trim(),
                    CreatedAt = creationTime,
                    LastModifiedAt = creationTime
                };

                // Save metadata as JSON.
                await using (var jsonStream = new FileStream(
                    metadataPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    metadataCreated = true;

                    await JsonSerializer.SerializeAsync(
                        jsonStream,
                        metadata,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                }

                return StatusCode(
                    StatusCodes.Status201Created,
                    new
                    {
                        message = "Image created successfully.",
                        fileName = fileName,
                        metadata = metadata
                    });
            }
            catch (Exception)
            {
                // Remove incomplete files if an error occurs.
                if (imageCreated && System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                if (metadataCreated &&
                    System.IO.File.Exists(metadataPath))
                {
                    System.IO.File.Delete(metadataPath);
                }

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Internal server error.");
            }
        }
    }
}