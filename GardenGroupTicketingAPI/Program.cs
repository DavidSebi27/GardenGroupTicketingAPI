using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using GardenGroupTicketingAPI.Models;
using GardenGroupTicketingAPI.Services;
using System.Text;

namespace GardenGroupTicketingAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI")
                ?? builder.Configuration["MongoDB:ConnectionURI"];
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? builder.Configuration["JwtSettings:SecretKey"];

            if (!string.IsNullOrEmpty(mongoUri))
                builder.Configuration["MongoDB:ConnectionURI"] = mongoUri;
            if (!string.IsNullOrEmpty(jwtSecret))
                builder.Configuration["JwtSettings:SecretKey"] = jwtSecret;

            builder.Services.Configure<MongoDBSettings>(
                builder.Configuration.GetSection("MongoDB"));

            builder.Services.Configure<JwtSettings>(
                builder.Configuration.GetSection("JwtSettings"));

            builder.Services.AddSingleton<IMongoDBService, MongoDBService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IPasswordHashingService, PasswordHashingService>();
            // Add services to the container.

            builder.Services.AddControllers();

            var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
            if (jwtSettings?.SecretKey == null)
            {
                throw new InvalidOperationException("JWT SecretKey is not configured.");
            }
            var key = Encoding.ASCII.GetBytes(jwtSettings!.SecretKey);

            builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = !builder.Environment.IsProduction();
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });



            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // Add a health check endpoint
            app.MapGet("/health", () => new {
                Status = "Healthy",
                Timestamp = DateTime.Now,
                Version = "2.0",
                Service = "Garden Group Incident Management API"
            });

            // API info endpoint
            app.MapGet("/api/info", () => new {
                name = "Garden Group Ticketing API",
                version = "2.0",
                description = "REST API for Garden Group ticket management system",
                endpoints = new
                {
                    health = "/health",
                    auth = "/api/auth",
                    employees = "/api/employees",
                    tickets = "/api/tickets",
                    dashboard = "/api/dashboard"
                }
            });

            // Temporary debug endpoint - REMOVE IN PRODUCTION
            app.MapPost("/debug/test-login", async (IMongoDBService mongoDb, IPasswordHashingService passwordService) =>
            {
                var employee = await mongoDb.GetEmployeeByNumberAsync(1000);

                if (employee == null)
                    return Results.Ok(new { error = "Employee 1000 not found!" });

                var passwordToTest = "manager123";
                bool isValid = passwordService.VerifyPassword(passwordToTest, employee.PasswordHash);

                return Results.Ok(new
                {
                    employeeFound = true,
                    employeeNumber = employee.EmployeeNumber,
                    firstName = employee.FirstName,
                    isActive = employee.IsActive,
                    passwordHash = employee.PasswordHash,
                    testPassword = passwordToTest,
                    passwordMatches = isValid
                });
            });

            app.MapPost("/test-post", () =>
            {
                return Results.Ok(new { message = "POST works!", timestamp = DateTime.UtcNow });
            });
            var port = Environment.GetEnvironmentVariable("PORT") ?? "5259";
            app.Run($"http://0.0.0.0:{port}");
        }
    }
}
