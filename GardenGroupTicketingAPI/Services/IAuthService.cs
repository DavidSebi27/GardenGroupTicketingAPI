using GardenGroupTicketingAPI.Models;

namespace GardenGroupTicketingAPI.Services
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<Employee?> RegisterEmployeeAsync(RegisterEmployeeRequest request);
    }
}