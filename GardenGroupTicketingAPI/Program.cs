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

            builder.Services.Configure<MongoDBSettings>(
                builder.Configuration.GetSection("MongoDB"));

            builder.Services.Configure<JwtSettings>(
                builder.Configuration.GetSection("JwtSettings"));

            builder.Services.AddSingleton<MongoDBService>();
            builder.Services.AddScoped<AuthService>();

            // Add services to the container.

            builder.Services.AddControllers();

            var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
            var key = Encoding.ASCII.GetBytes(jwtSettings!.SecretKey);

            builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = false; // Set to true in production
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
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Service = "Garden Group Incident Management API"
            });

            app.Run();
        }
    }
}
