using GardenGroupTicketingAPI.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GardenGroupTicketingAPI.Services
{
    public class AuthService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly MongoDBService _mongoDBService;

        public AuthService(IOptions<JwtSettings> jwtSettings, MongoDBService mongoDBService)
        {
            _jwtSettings = jwtSettings.Value;
            _mongoDBService = mongoDBService;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var employee = await _mongoDBService.GetEmployeeAsync(request.EmployeeId);

            if (employee == null)
            {
                return null;
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, employee.PasswordHash))
            {
                return null;
            }

            //var token = GenerateJwtToken(employee);

            return new LoginResponse
            {
                Token = token,
                Employee = employee
            };
        }

        /*public async Task<Employee?> RegisterEmployeeAsync(RegisterEmployeeRequest request)
        {
            if (await _mongoDBService.EmailExistsAsync(request.Email))
            {
                return null;
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword()
        }*/
    }
}
