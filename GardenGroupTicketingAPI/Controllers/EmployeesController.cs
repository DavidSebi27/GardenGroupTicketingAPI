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
            var employee = await _mongoDbService.GetEmployeeByIdAsync(id);
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
            var employee = await _mongoDbService.GetEmployeeByIdAsync(userId);

            if (employee == null)
            {
                return NotFound(new { message = "Employee not found." });
            }

            return Ok(employee);
        }

        [HttpGet("by-number/{employeeNumber}")]
        public async Task<IActionResult> GetEmployeeByNumber(int employeeNumber)
        {
            if (!AuthService.IsServiceDeskEmployee(User))
            {
                return Forbid();
            }

            var employee = await _mongoDbService.GetEmployeeByNumberAsync(employeeNumber);
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

            if (await _mongoDbService.EmployeeNumberExistsAsync(request.EmployeeNumber))
            {
                return BadRequest(new { message = "Employee with this employee number already exists." });
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
                Company = request.Company,
                EmployeeNumber = request.EmployeeNumber,
                PasswordHash = passwordHash,
                AccessLevel = request.AccessLevel,
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            await _mongoDbService.CreateEmployeeAsync(employee);
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(string id, [FromBody] RegisterEmployeeRequest request)
        {
            if (!AuthService.IsManager(User))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingEmployee = await _mongoDbService.GetEmployeeByIdAsync(id);
            if (existingEmployee == null)
            {
                return NotFound(new { message = "Employee not found." });
            }

            // Check if email is being changed to one that already exists
            if (existingEmployee.Email != request.Email && await _mongoDbService.EmailExistsAsync(request.Email))
            {
                return BadRequest(new { message = "Employee with this email already exists." });
            }

            // Check if employee number is being changed to one that already exists
            if (existingEmployee.EmployeeNumber != request.EmployeeNumber && await _mongoDbService.EmployeeNumberExistsAsync(request.EmployeeNumber))
            {
                return BadRequest(new { message = "Employee with this employee number already exists." });
            }

            // Update employee properties
            existingEmployee.FirstName = request.FirstName;
            existingEmployee.LastName = request.LastName;
            existingEmployee.Email = request.Email;
            existingEmployee.Department = request.Department;
            existingEmployee.PhoneNumber = request.PhoneNumber;
            existingEmployee.Address = request.Address;
            existingEmployee.Company = request.Company;
            existingEmployee.EmployeeNumber = request.EmployeeNumber;
            existingEmployee.AccessLevel = request.AccessLevel;

            // Only update password if provided
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                existingEmployee.PasswordHash = _passwordHashingService.HashPassword(request.Password);
            }

            await _mongoDbService.UpdateEmployeeAsync(id, existingEmployee);
            return Ok(existingEmployee);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(string id)
        {
            if (!AuthService.IsManager(User))
            {
                return Forbid();
            }

            var employee = await _mongoDbService.GetEmployeeByIdAsync(id);
            if (employee == null)
            {
                return NotFound(new { message = "Employee not found." });
            }

            var userId = AuthService.GetUserIdFromClaims(User);
            if (userId == id)
            {
                return BadRequest(new { message = "Cannot delete your own account." });
            }

            await _mongoDbService.DeleteEmployeeAsync(id);
            return NoContent();
        }
    }
}
