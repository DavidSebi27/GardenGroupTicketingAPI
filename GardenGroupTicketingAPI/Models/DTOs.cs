using GardenGroupTicketingAPI.Models;

namespace GardenGroupTicketingAPI.Models
{
    public class DTOs
    {
        // Authentication DTOs
        public class LoginRequest
        {
            public string Username { get; set; } = null!;
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
            public string Department { get; set; } = null!;
            public string? PhoneNumber { get; set; }
            public Address? Address { get; set; }
            public string Username { get; set; } = null!;
            public EmployeeRole Role { get; set; } = EmployeeRole.RegularEmployee;
        }
    }
}
