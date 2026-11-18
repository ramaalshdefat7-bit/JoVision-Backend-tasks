using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace JoVisionBackendTasks.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FilterController : ControllerBase
    {
        [HttpPost]
        public IActionResult Filter(
            [FromForm] DateTime? CreationDate,
            [FromForm] DateTime? ModificationDate,
            [FromForm] string? Owner,
            [FromForm] FilterType? FilterType)
        {
            try
            {
                if (FilterType == null ||
                    !Enum.IsDefined(typeof(FilterType), FilterType.Value))
                {
                    return BadRequest("Invalid FilterType.");
                }

                if (FilterType == JoVisionBackendTasks.FilterType.ByModificationDate &&
                    ModificationDate == null)
                {
                    return BadRequest("ModificationDate is required.");
                }

                if ((FilterType == JoVisionBackendTasks.FilterType.ByCreationDateDescending ||
                     FilterType == JoVisionBackendTasks.FilterType.ByCreationDateAscending) &&
                    CreationDate == null)
                {
                    return BadRequest("CreationDate is required.");
                }

                if (FilterType == JoVisionBackendTasks.FilterType.ByOwner &&
                    string.IsNullOrWhiteSpace(Owner))
                {
                    return BadRequest("Owner is required.");
                }

                string folderPath = Path.Combine(
                    Directory.GetCurrentDirectory(), "UploadedImages");

                if (!Directory.Exists(folderPath))
                {
                    return Ok(Array.Empty<JsonData>());
                }

                var files = new List<(JsonData Data, ImageMetadata Metadata)>();

                foreach (string imagePath in Directory.GetFiles(folderPath, "*.jpg"))
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

                    files.Add((
                        new JsonData
                        {
                            FileName = Path.GetFileName(imagePath),
                            Owner = metadata.Owner
                        },
                        metadata
                    ));
                }

                IEnumerable<(JsonData Data, ImageMetadata Metadata)> filteredFiles;

                switch (FilterType.Value)
                {
                    case JoVisionBackendTasks.FilterType.ByModificationDate:
                        filteredFiles = files.Where(file =>
                            file.Metadata.LastModifiedAt < ModificationDate!.Value);
                        break;

                    case JoVisionBackendTasks.FilterType.ByCreationDateDescending:
                        filteredFiles = files
                            .Where(file => file.Metadata.CreatedAt > CreationDate!.Value)
                            .OrderByDescending(file => file.Metadata.CreatedAt);
                        break;

                    case JoVisionBackendTasks.FilterType.ByCreationDateAscending:
                        filteredFiles = files
                            .Where(file => file.Metadata.CreatedAt > CreationDate!.Value)
                            .OrderBy(file => file.Metadata.CreatedAt);
                        break;

                    case JoVisionBackendTasks.FilterType.ByOwner:
                        filteredFiles = files.Where(file =>
                            string.Equals(
                                file.Metadata.Owner,
                                Owner!.Trim(),
                                StringComparison.OrdinalIgnoreCase));
                        break;

                    default:
                        return BadRequest("Invalid FilterType.");
                }

                var result = filteredFiles
                    .Select(file => file.Data)
                    .ToArray();

                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}