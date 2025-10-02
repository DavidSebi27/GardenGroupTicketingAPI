using GardenGroupTicketingAPI.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GardenGroupTicketingAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IMongoDBService _mongoDBService;
        private readonly IPasswordHashingService _passwordHashingService;

        public AuthService(IOptions<JwtSettings> jwtSettings, IMongoDBService mongoDBService, IPasswordHashingService passwordHashingService)
        {
            _jwtSettings = jwtSettings.Value;
            _mongoDBService = mongoDBService;
            _passwordHashingService = passwordHashingService;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var employee = await _mongoDBService.GetEmployeeByNumberAsync(request.EmployeeNumber);

            if (employee == null || !employee.IsActive)
            {
                return null;
            }

            if (!_passwordHashingService.VerifyPassword(request.Password, employee.PasswordHash))
            {
                return null;
            }

            var token = GenerateJwtToken(employee);

            return new LoginResponse
            {
                Token = token,
                Employee = employee
            };
        }

        public async Task<Employee?> RegisterEmployeeAsync(RegisterEmployeeRequest request)
        {
            if (await _mongoDBService.EmailExistsAsync(request.Email))
            {
                return null;
            }

            if (await _mongoDBService.EmployeeNumberExistsAsync(request.EmployeeNumber))
            {
                return null;
            }

            var passwordHash = _passwordHashingService.HashPassword(request.Password);

            var employee = new Employee
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Department = request.Department,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                Company = request.Company,
                EmployeeNumber = request.EmployeeNumber,
                PasswordHash = passwordHash,
                AccessLevel = request.AccessLevel,
                IsActive = true,
                CreatedDate = DateTime.Now,
            };

            await _mongoDBService.CreateEmployeeAsync(employee);
            return employee;
        }

        private string GenerateJwtToken(Employee employee)
        {
            var tokenHandler = new JwtSecurityTokenHandler(); 
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

            var claims = new List<Claim> // claims are pieces of info known about the user, like fields on a drivers license
            {
                new Claim(ClaimTypes.NameIdentifier, employee.Id!),
                new Claim(ClaimTypes.Email, employee.Email),
                new Claim(ClaimTypes.Name, $"{employee.FirstName} {employee.LastName}"),
                new Claim(ClaimTypes.Role, employee.AccessLevel.ToString()),
                new Claim("Department", employee.Department),
                new Claim("Company", employee.Company),
                new Claim("EmployeeNumber", employee.EmployeeNumber.ToString()),
                new Claim("AccessLevel", employee.AccessLevel.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMinutes(_jwtSettings.ExpiryMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public static string GetUserIdFromClaims(ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        public static int GetEmployeeNumberFromClaims(ClaimsPrincipal user)
        {
            var employeeNumberStr = user.FindFirstValue("EmployeeNumber");
            return int.TryParse(employeeNumberStr, out var employeeNumber) ? employeeNumber : 0;
        }

        public static string GetDepartmentFromClaims(ClaimsPrincipal user)
        {
            return user.FindFirstValue("Department") ?? string.Empty;
        }

        public static string GetCompanyFromClaims(ClaimsPrincipal user)
        {
            return user.FindFirstValue("Company") ?? string.Empty;
        }

        public static int GetAccessLevelFromClaims(ClaimsPrincipal user)
        {
            var accessLevelStr = user.FindFirstValue("AccessLevel");
            return int.TryParse(accessLevelStr, out var accessLevel) ? accessLevel : 1;
        }

        public static bool IsServiceDeskEmployee(ClaimsPrincipal user)
        {
            var accessLevel = GetAccessLevelFromClaims(user);
            return accessLevel >= 2; // 2=ServiceDesk, 3=Manager (both have service desk permissions)
        }

        public static bool IsManager(ClaimsPrincipal user)
        {
            var accessLevel = GetAccessLevelFromClaims(user);
            return accessLevel == 3; // 3=Manager
        }

        public static bool IsRegularEmployee(ClaimsPrincipal user)
        {
            var accessLevel = GetAccessLevelFromClaims(user);
            return accessLevel == 1; // 1=Regular
        }

        public static string GetAccessLevelName(int accessLevel)
        {
            return accessLevel switch
            {
                1 => "Regular Employee",
                2 => "Service Desk",
                3 => "Manager",
                _ => "Unknown"
            };
        }
    }
}
