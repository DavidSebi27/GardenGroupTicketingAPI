using GardenGroupTicketingAPI.Models;
using GardenGroupTicketingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;

namespace GardenGroupTicketingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly IMongoDBService _mongoDBService;

        public TicketsController(IMongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTickets()
        {
            if (AuthService.IsManager(User))
            {
                var allTickets = await _mongoDBService.GetTicketsAsync();
                return Ok(allTickets);
            }

            if (AuthService.IsServiceDeskEmployee(User))
            {
                var userId = AuthService.GetUserIdFromClaims(User);
                var assignedTickets = await _mongoDBService.GetTicketsAssignedToEmployeeAsync(userId);
                return Ok(assignedTickets);
            }

            var employeeNumber = AuthService.GetEmployeeNumberFromClaims(User);
            var userTickets = await _mongoDBService.GetTicketsByEmployeeNumberAsync(employeeNumber);
            return Ok(userTickets);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(string id)
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
            if (!AuthService.IsServiceDeskEmployee(User))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                ModelState.AddModelError("Description", Constants.ErrorMessages.InvalidDescription);
                return BadRequest(ModelState);
            }

            if (request.PriorityLevel < Constants.PriorityLevels.Min ||
                request.PriorityLevel > Constants.PriorityLevels.Max)
            {
                ModelState.AddModelError("PriorityLevel", $"Priority level must be between {Constants.PriorityLevels.Min} (Low) and {Constants.PriorityLevels.Max} (Critical)");
                return BadRequest(ModelState);
            }

            if (request.Deadline.HasValue && request.Deadline.Value < DateTime.UtcNow)
            {
                ModelState.AddModelError("Deadline", Constants.ErrorMessages.DeadlineInPast);
                return BadRequest(ModelState);
            }

            var reportingEmployee = await _mongoDBService.GetEmployeeByNumberAsync(request.EmployeeNumber);
            if (reportingEmployee == null)
            {
                return Unauthorized(new { message = Constants.ErrorMessages.EmployeeNotFound});
            }

            var ticket = new Ticket
            {
                Description = request.Description.Trim(),
                PriorityLevel = request.PriorityLevel ?? Constants.PriorityLevels.Default,
                Deadline = request.Deadline,
                Status = TicketStatus.open,
                Date = DateTime.Now,
                ReportedBy = new ReportedByEmployee
                {
                    FirstName = reportingEmployee.FirstName,
                    LastName = reportingEmployee.LastName,
                    Email = reportingEmployee.Email,
                    Department = reportingEmployee.Department,
                    PhoneNumber = reportingEmployee.PhoneNumber,
                    Company = reportingEmployee.Company,
                    EmployeeNumber = reportingEmployee.EmployeeNumber
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
                return NotFound(new { message = Constants.ErrorMessages.TicketNotFound });
            }

            // Additional validation for deadline
            if (request.Deadline.HasValue && request.Deadline.Value < DateTime.UtcNow)
            {
                ModelState.AddModelError("Deadline", Constants.ErrorMessages.DeadlineInPast);
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
            var ticket = await _mongoDBService.GetTicketAsync(id);
            if (ticket == null)
            {
                return NotFound(new { message = Constants.ErrorMessages.TicketNotFound});
            }

            var userId = AuthService.GetUserIdFromClaims(User);
            var employeeNumber = AuthService.GetEmployeeNumberFromClaims(User);
            var isManager = AuthService.IsManager(User);
            var isServiceDesk = AuthService.IsServiceDeskEmployee(User);

            // Check if user has permission to delete this ticket
            var canDelete = isManager ||
                            (isServiceDesk && ticket.ContactPerson == userId) ||
                            (!isServiceDesk && ticket.ReportedBy.EmployeeNumber == employeeNumber);

            if (!canDelete)
            {
                return Forbid();
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

        [HttpGet("assigned-to-me")]
        public async Task<IActionResult> GetMyAssignedTickets()
        {
            if (!AuthService.IsServiceDeskEmployee(User))
            {
                return Forbid();
            }

            var userId = AuthService.GetUserIdFromClaims(User);
            var tickets = await _mongoDBService.GetTicketsAssignedToEmployeeAsync(userId);
            return Ok(tickets);
        }

        // this is a running thought, i dont know if this is how it should be implemented.
        [HttpPost("{id}/assign")]
        public async Task<IActionResult> AssignTicket(string id, [FromBody] AssignTicketRequest request)
        {
            if (!AuthService.IsServiceDeskEmployee(User))
            {
                return Forbid();
            }

            var ticket = await _mongoDBService.GetTicketAsync(id);
            if (ticket == null)
            {
                return NotFound(new { message = Constants.ErrorMessages.TicketNotFound });
            }

            var assignee = await _mongoDBService.GetEmployeeByIdAsync(request.AssigneeId);
            if (assignee == null)
            {
                return BadRequest(new { message = "Assignee not found." });
            }

            if (assignee.AccessLevel < Constants.AccessLevels.ServiceDesk)
            {
                return BadRequest(new { message = "Cannot assign ticket to employee without Service Desk access." });
            }

            var updateRequest = new UpdateTicketRequest
            {
                AssignedTo = request.AssigneeId,
                Status = "inProgress"
            };

            var userId = AuthService.GetUserIdFromClaims(User);
            await _mongoDBService.UpdateTicketAsync(id, updateRequest, userId);

            var updatedTicket = await _mongoDBService.GetTicketAsync(id);
            return Ok(updatedTicket);
        }
    }
}
