using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace GardenGroupTicketingAPI.Models
{
    public class Employee
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        [BsonElement("first_name")]
        public string FirstName { get; set; } = null!;
        
        [BsonElement("last_name")]
        public string LastName { get; set; } = null!;
        
        [BsonElement("email")]
        public string Email { get; set; } = null!;
        
        [BsonElement("department")]
        public string Department { get; set; } = null!;
        
        [BsonElement("phone_number")]
        public string? PhoneNumber { get; set; }
        
        [BsonElement("access_level")]
        public int AccessLevel { get; set; } = Constants.AccessLevels.Regular;
        
        [BsonElement("address")]
        public Address? Address { get; set; }

        [BsonElement("company")]
        public string Company { get; set; } = null!;

        [BsonElement("employee_nr")]
        public int EmployeeNumber { get; set; }
        
        [BsonElement("password_hash")]
        [JsonIgnore]
        public string PasswordHash { get; set; } = null!;
        
        [BsonElement("is_active")]
        public bool IsActive { get; set; } = true;
        
        [BsonElement("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;    
    }
    public class Address
    {
        [BsonElement("street")]
        public string? Street { get; set; }

        [BsonElement("house_number")]
        public int? HouseNumber { get; set; }

        [BsonElement("city")]
        public string? City { get; set; }

        [BsonElement("postal_code")]
        public string? PostalCode { get; set; }
    }

    public enum AccessLevel
    {
        Regular = 1,
        ServiceDesk = 2,
        Manager = 3
    }
}
