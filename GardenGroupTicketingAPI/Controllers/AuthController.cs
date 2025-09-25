using GardenGroupTicketingAPI.Models;
using GardenGroupTicketingAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GardenGroupTicketingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(request);
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid Employee ID or Password" });
            }

            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterEmployeeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var employee = await _authService.RegisterEmployeeAsync(request);
            if (employee == null)
            {
                return BadRequest(new { message = "Employee with this email already exists" });
            }

            return CreatedAtAction("GetEmployee", "Employees", new { id = employee.Id },
                new { message = "Employee registered successfully!", employee });
        }
    }
}
