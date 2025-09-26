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
            // Only service desk employees can access this dashboard
            if (!AuthService.IsServiceDeskEmployee(User))
                return Forbid();

            var dashboard = await _mongoDBService.GetServiceDeskDashboardAsync();
            return Ok(dashboard);
        }
    }
}
