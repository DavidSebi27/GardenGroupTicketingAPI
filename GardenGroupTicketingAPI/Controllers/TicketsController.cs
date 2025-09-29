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

            var employeeNumber = AuthService.GetEmployeeNumberFromClaims(User);
            var userTickets = await _mongoDBService.GetTicketsByEmployeeNumberAsync(employeeNumber);
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

            var employeeNumber = AuthService.GetEmployeeNumberFromClaims(User); // may i get your identification good sir?
            if (!AuthService.IsServiceDeskEmployee(User) && ticket.ReportedBy.EmployeeNumber != employeeNumber)
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

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                ModelState.AddModelError("Description", "Description cannot be empty or whitespace only");
                return BadRequest(ModelState);
            }

            if (request.PriorityLevel < 1 || request.PriorityLevel > 4)
            {
                ModelState.AddModelError("PriorityLevel", "Priority level must be between 1 (Low) and 4 (Critical)");
                return BadRequest(ModelState);
            }

            if (request.Deadline.HasValue && request.Deadline.Value < DateTime.UtcNow)
            {
                ModelState.AddModelError("Deadline", "Deadline cannot be in the past");
                return BadRequest(ModelState);
            }

            var userId = AuthService.GetUserIdFromClaims(User);
            var currentEmployee = await _mongoDBService.GetEmployeeByIdAsync(userId);
            if (currentEmployee == null)
            {
                return Unauthorized(new { message = "Employee not found."});
            }

            var ticket = new Ticket
            {
                Description = request.Description.Trim(),
                PriorityLevel = (int)request.PriorityLevel,
                Deadline = request.Deadline,
                Status = TicketStatus.open,
                Date = DateTime.Now,
                ReportedBy = new ReportedByEmployee
                {
                    FirstName = currentEmployee.FirstName,
                    LastName = currentEmployee.LastName,
                    Email = currentEmployee.Email,
                    Department = currentEmployee.Department,
                    PhoneNumber = currentEmployee.PhoneNumber,
                    Company = currentEmployee.Company,
                    EmployeeNumber = currentEmployee.EmployeeNumber
                }
            };

            await _mongoDBService.CreateTicketAsync(ticket);
            return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicket(string id, [FromBody] UpdateTicketRequest request)
        {
            if (!AuthService.IsServiceDeskEmployee(User))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var ticket = await _mongoDBService.GetTicketAsync(id);
            if (ticket == null)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            // Additional validation for deadline
            if (request.Deadline.HasValue && request.Deadline.Value < DateTime.UtcNow)
            {
                ModelState.AddModelError("Deadline", "Deadline cannot be in the past");
                return BadRequest(ModelState);
            }

            var userId = AuthService.GetUserIdFromClaims(User);
            await _mongoDBService.UpdateTicketAsync(id, request, userId);

            // Return updated ticket
            var updatedTicket = await _mongoDBService.GetTicketAsync(id);
            return Ok(updatedTicket);
        }


        [HttpDelete("{id}")] 
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

        [HttpGet("by-employee/{employeeNumber}")]
        public async Task<IActionResult> GetTicketsByEmployee(int employeeNumber)
        {
            if (!AuthService.IsServiceDeskEmployee(User))
            {
                return Forbid();
            }

            var tickets = await _mongoDBService.GetTicketsByEmployeeNumberAsync(employeeNumber);
            return Ok(tickets);
        }

        // this is a running thought, i dont know if this is how it should be implemented.
        [HttpPost("{id}/assign")]
        public async Task<IActionResult> AssignTicket(string id, [FromBody] string assigneeId)
        {
            if (!AuthService.IsServiceDeskEmployee(User))
            {
                return Forbid();
            }

            var ticket = await _mongoDBService.GetTicketAsync(id);
            if (ticket == null)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            var assignee = await _mongoDBService.GetEmployeeByIdAsync(assigneeId);
            if (assignee == null)
            {
                return BadRequest(new { message = "Assignee not found." });
            }

            var updateRequest = new UpdateTicketRequest
            {
                AssignedTo = assigneeId,
                Status = "inProgress"
            };

            var userId = AuthService.GetUserIdFromClaims(User);
            await _mongoDBService.UpdateTicketAsync(id, updateRequest, userId);

            var updatedTicket = await _mongoDBService.GetTicketAsync(id);
            return Ok(updatedTicket);
        }
    }
}
