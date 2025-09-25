using GardenGroupTicketingAPI.Models;

namespace GardenGroupTicketingAPI.Models
{

    // Authentication DTOs
    public class LoginRequest
    {
        public string EmployeeId { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = null!;
        public Employee Employee { get; set; } = null!;
    }

    public class RegisterEmployeeRequest
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Department { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public Address? Address { get; set; }
        public string EmployeeId { get; set; } = null!;
        public int AccessLevel { get; set; } = 1;
    }
    public class CreateTicketRequest
    {
        public string Description { get; set; } = null!;
        public double? PriorityLevel { get; set; } = 2;
        public DateTime? Deadline { get; set; }
    }
    public class UpdateTicketRequest
    {
        public string Description { get; set; }
        //public string? Title { get; set; } could have title??
        public double? PriorityLevel { get; set; }
        public string? Status { get; set; }
        public DateTime? Deadline { get; set; }
        public string? AssignedTo { get; set; }
    }

    public class ChangePasswordRequest // PREPARING FOR PATO IDK IF THIS WILL BE USED OR NOT? IF NOT, I WILL DELETE THIS
    {
        public string? CurrentPassword { get; set; }
        public string NewPassword { get; set; } = null!;
    }
    public class DashboardResponse
    {
        public int TotalTickets { get; set; }
        public double OpenPercentage { get; set; }
        public double ResolvedPercentage { get; set; }
        public double ClosedWithoutResolvePercentage { get; set; }
        public Dictionary<string, int>? TicketsByPriority { get; set; } // sorting
    }
}
