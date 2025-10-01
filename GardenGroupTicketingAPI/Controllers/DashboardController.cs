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

        // Single endpoint that routes to the correct dashboard based on role
        // Manager: All tickets statistics
        // Service Desk: Tickets assigned to them statistics
        // Regular: Tickets they reported statistics
        
        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = AuthService.GetUserIdFromClaims(User);

            // Managers see all tickets statistics
            if (AuthService.IsManager(User))
            {
                var managerDashboard = await _mongoDBService.GetManagerDashboardAsync();
                return Ok(managerDashboard);
            }

            // Service desk employees see statistics for tickets assigned to them
            if (AuthService.IsServiceDeskEmployee(User))
            {
                var serviceDeskDashboard = await _mongoDBService.GetServiceDeskDashboardAsync(userId);
                return Ok(serviceDeskDashboard);
            }

            // Regular employees see statistics for tickets they reported
            var employeeDashboard = await _mongoDBService.GetEmployeeDashboardAsync(userId);
            return Ok(employeeDashboard);
        }

        [HttpGet("employee/{employeeNumber}")]
        public async Task<IActionResult> GetEmployeeDashboardByNumber(int employeeNumber)
        {
            if (!AuthService.IsManager(User))
            {
                return Forbid();
            }

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
