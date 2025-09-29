using GardenGroupTicketingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GardenGroupTicketingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        public DashboardController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        [HttpGet("employee")]
        public async Task<IActionResult> GetEmployeeDashboard()
        {
            var userId = AuthService.GetUserIdFromClaims(User);
            var dashboard = await _mongoDBService.GetEmployeeDashboardAsync(userId);
            return Ok(dashboard);
        }

        [HttpGet("servicedesk")]
        public async Task<IActionResult> GetServiceDeskDashboard()
        {
            if (!AuthService.IsServiceDeskEmployee(User))
                return Forbid();

            var dashboard = await _mongoDBService.GetServiceDeskDashboardAsync();
            return Ok(dashboard);
        }

        [HttpGet("employee/{employeeNumber}")]
        public async Task<IActionResult> GetEmployeeDashboardByNumber(int employeeNumber)
        {
            if (!AuthService.IsServiceDeskEmployee(User))
                return Forbid();

            var employee = await _mongoDBService.GetEmployeeByNumberAsync(employeeNumber);
            if (employee == null)
            {
                return NotFound(new { message = "Employee not found." });
            }

            var dashboard = await _mongoDBService.GetEmployeeDashboardAsync(employee.Id!);
            return Ok(dashboard);
        }
    }
}
