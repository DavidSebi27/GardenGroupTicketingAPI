using GardenGroupTicketingAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace GardenGroupTicketingAPI.Models
{

    // Authentication DTOs
    public class LoginRequest
    {
        [Required(ErrorMessage = "Employee number is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Employee number must be a positive number.")]
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
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = null!;
        
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = null!;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = null!;
        
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        public string Password { get; set; } = null!;
        
        [Required(ErrorMessage = "Department is required")]
        [StringLength(50, ErrorMessage = "Department cannot exceed 50 characters")]
        public string Department { get; set; } = null!;
        
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; set; }
        
        public Address? Address { get; set; }
        
        [Required(ErrorMessage = "Employee number is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Employee number must be a positive integer")]
        public int EmployeeNumber { get; set; }
        
        [Range(1, 3, ErrorMessage = "Access level must be between 1 and 3")]
        public int AccessLevel { get; set; } = 1;
    }
    public class CreateTicketRequest
    {
        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 1000 characters")]
        public string Description { get; set; } = null!;
        
        [Range(1, 4, ErrorMessage = "Priority level must be between 1 (Low) and 4 (Critical)")]
        public int? PriorityLevel { get; set; } = 2;

        [DataType(DataType.DateTime)]
        public DateTime? Deadline { get; set; }
    }
    public class UpdateTicketRequest
    {
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 1000 characters")]
        public string Description { get; set; }

        [Range(1, 4, ErrorMessage = "Priority level must be between 1 (Low) and 4 (Critical)")]
        public int? PriorityLevel { get; set; }

        [RegularExpression("^(open|inProgress|resolved|closed)$", ErrorMessage = "Status must be one of: open, inProgress, resolved, closed")]
        public string? Status { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? Deadline { get; set; }

        public string? AssignedTo { get; set; }

        [StringLength(1000, ErrorMessage = "Resolution notes cannot exceed 1000 characters")]
        public string? ResolutionNotes { get; set; }
    }

    public class ChangePasswordRequest // PREPARING FOR PATO IDK IF THIS WILL BE USED OR NOT? IF NOT, I WILL DELETE THIS
    {
        public string? CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
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
