using GardenGroupTicketingAPI.Models;
using GardenGroupTicketingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace GardenGroupTicketingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        public TicketsController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTickets()
        {
            if (AuthService.IsServiceDeskEmployee(User))
            {
                var allTickets = await _mongoDBService.GetTicketsAsync();
                return Ok(allTickets);
            }

            var userId = AuthService.GetUserIdFromClaims(User);
            var userTickets = await _mongoDBService.GetTicketsByEmployeeAsync(userId);
            return Ok(userTickets);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(string id) // get ticket by id
        {
            var ticket = await _mongoDBService.GetTicketAsync(id);
            if (ticket == null)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            var userId = AuthService.GetUserIdFromClaims(User); // may i get your identification good sir?
            if (!AuthService.IsServiceDeskEmployee(User) && ticket.ReportedBy.EmployeeId != userId)
            {
                return Forbid();
            }

            return Ok(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = AuthService.GetUserIdFromClaims(User);
            var userName = User.Identity?.Name ?? "Unknown";
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            var department = AuthService.GetDepartmentFromClaims(User);

            var ticket = new Ticket
            {
                Description = request.Description,
                PriorityLevel = (double)request.PriorityLevel,
                Deadline = request.Deadline,
                Status = TicketStatus.open,
                Date = DateTime.Now,
                ReportedBy = new Employee
                {
                    EmployeeId = userId,
                    FirstName = userName,
                    Email = userEmail,
                    Department = department
                }
            };

            await _mongoDBService.CreateTicketAsync(ticket);
            return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket);
        }

        // note to self: add ticket update func
        // note to self from other self: fuck you, add delete func

        [HttpDelete("{id}")] // note to self: delete the above comment
        public async Task<IActionResult> DeleteTicket(string id)
        {
            if (!AuthService.IsServiceDeskEmployee(User))
            {
                return Forbid();
            }

            var ticket = await _mongoDBService.GetTicketAsync(id);
            if (ticket == null)
            {
                return NotFound(new { message = "Ticket not found."});
            }

            await _mongoDBService.DeleteTicketAsync(id);
            return NoContent();
        }
    }
}
