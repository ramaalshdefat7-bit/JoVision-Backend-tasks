using Microsoft.AspNetCore.Mvc;

namespace JoVisionBackendTasks.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BirthDateController : ControllerBase
    {
        // Task 45: GET request using Query Parameters
        [HttpGet]
        public IActionResult Get(
            [FromQuery] string? name,
            [FromQuery] int? years,
            [FromQuery] int? months,
            [FromQuery] int? days)
        {
            return CalculateAge(name, years, months, days);
        }

        // Task 46: POST request using FormData
        [HttpPost]
        public IActionResult Post(
            [FromForm] string? name,
            [FromForm] int? years,
            [FromForm] int? months,
            [FromForm] int? days)
        {
            return CalculateAge(name, years, months, days);
        }

        // Calculate the age in completed years.
        private IActionResult CalculateAge(
            string? name,
            int? years,
            int? months,
            int? days)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "anonymous";
            }

            if (years == null || months == null || days == null)
            {
                return Content(
                    $"Hello {name}, I can't calculate your age without knowing your birthdate!");
            }

            try
            {
                DateTime birthDate = new DateTime(
                    years.Value,
                    months.Value,
                    days.Value
                );

                DateTime today = DateTime.Today;

                if (birthDate.Date > today)
                {
                    return BadRequest("Birthdate cannot be in the future.");
                }

                int age = today.Year - birthDate.Year;

                if (birthDate.Date > today.AddYears(-age))
                {
                    age--;
                }

                return Content($"Hello {name}, your age is {age}");
            }
            catch (ArgumentOutOfRangeException)
            {
                return BadRequest("Invalid birthdate.");
            }
        }
    }
}