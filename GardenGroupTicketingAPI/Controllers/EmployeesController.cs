using GardenGroupTicketingAPI.Models;
using GardenGroupTicketingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GardenGroupTicketingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly MongoDBService _mongoDbService;
        private readonly IPasswordHashingService _passwordHashingService;

        public EmployeesController(MongoDBService mongoDbService, IPasswordHashingService passwordHashingService)
        {
            _mongoDbService = mongoDbService;
            _passwordHashingService = passwordHashingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            if (!AuthService.IsServiceDeskEmployee(User))
            {
                return Forbid();
            }

            var employees = await _mongoDbService.GetEmployeesAsync();
            return Ok(employees);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployee(string id)
        {
            var employee = await _mongoDbService.GetEmployeeAsync(id);
            if (employee == null)
            {
                return NotFound(new { message = "Employee not found." });
            }

            var userId = AuthService.GetUserIdFromClaims(User);
            if (!AuthService.IsServiceDeskEmployee(User) && userId != id)
            {
                return Forbid();
            }

            return Ok(employee);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentEmployee()
        {
            var userId = AuthService.GetUserIdFromClaims(User);
            var employee = await _mongoDbService.GetEmployeeAsync(userId);

            if (employee == null)
            {
                return NotFound(new { message = "Employee not found." });
            }

            return Ok(employee);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] RegisterEmployeeRequest request)
        {
            if (!AuthService.IsManager(User))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _mongoDbService.EmailExistsAsync(request.Email))
            {
                return BadRequest(new { message = "Employee with this email already exists."});
            }

            var passwordHash = _passwordHashingService.HashPassword(request.Password);

            var employee = new Employee
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Department = request.Department,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                PasswordHash = passwordHash,
                AccessLevel = request.AccessLevel,
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            await _mongoDbService.CreateEmployeeAsync(employee);
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }

        // note to self: add edit/update employee please

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(string id)
        {
            if (!AuthService.IsManager(User))
            {
                return Forbid();
            }

            var employee = await _mongoDbService.GetEmployeeAsync(id);
            if (employee == null)
            {
                return NotFound(new { message = "Employee not found." });
            }

            var userId = AuthService.GetUserIdFromClaims(User);
            if (userId == id)
            {
                return BadRequest(new { message = "Cannot delete your own account." });
            }

            await _mongoDbService.DeleteEmployeeAsync(userId);
            return NoContent();
        }

        //note to self 2: implement proper password changing (if thats my job even..)
    }
}
