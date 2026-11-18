using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace JoVisionBackendTasks.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TransferOwnershipController : ControllerBase
    {
        [HttpGet]
        public IActionResult TransferOwnership(
            [FromQuery] string? OldOwner,
            [FromQuery] string? NewOwner)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(OldOwner) ||
                    string.IsNullOrWhiteSpace(NewOwner))
                {
                    return BadRequest("OldOwner and NewOwner are required.");
                }

                string folderPath = Path.Combine(
                    Directory.GetCurrentDirectory(), "UploadedImages");

                if (!Directory.Exists(folderPath))
                {
                    return Ok(Array.Empty<JsonData>());
                }

                string oldOwner = OldOwner.Trim();
                string newOwner = NewOwner.Trim();

                string[] imageFiles = Directory.GetFiles(folderPath, "*.jpg");

                foreach (string imagePath in imageFiles)
                {
                    string metadataPath = imagePath + ".json";

                    if (!System.IO.File.Exists(metadataPath))
                    {
                        return StatusCode(500, "Image metadata file is missing.");
                    }

                    string json = System.IO.File.ReadAllText(metadataPath);

                    ImageMetadata? metadata =
                        JsonSerializer.Deserialize<ImageMetadata>(json);

                    if (metadata == null)
                    {
                        return StatusCode(500, "Invalid image metadata.");
                    }

                    if (string.Equals(
                        metadata.Owner,
                        oldOwner,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        metadata.Owner = newOwner;

                        string updatedJson =
                            JsonSerializer.Serialize(metadata,
                                new JsonSerializerOptions
                                {
                                    WriteIndented = true
                                });

                        System.IO.File.WriteAllText(metadataPath, updatedJson);
                    }
                }

                var result = new List<JsonData>();

                foreach (string imagePath in imageFiles)
                {
                    string metadataPath = imagePath + ".json";

                    string json = System.IO.File.ReadAllText(metadataPath);

                    ImageMetadata? metadata =
                        JsonSerializer.Deserialize<ImageMetadata>(json);

                    if (metadata != null &&
                        string.Equals(
                            metadata.Owner,
                            newOwner,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(new JsonData
                        {
                            FileName = Path.GetFileName(imagePath),
                            Owner = metadata.Owner
                        });
                    }
                }

                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}