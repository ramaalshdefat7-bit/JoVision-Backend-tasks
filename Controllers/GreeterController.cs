using Microsoft.AspNetCore.Mvc;

namespace JoVisionBackendTasks.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GreeterController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get([FromQuery] string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "anonymous";
            }

            return Content($"Hello {name}");
        }
    }
}