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
        public int AccessLevel { get; set; } = 1;
        [BsonElement("address")]
        public Address? Address { get; set; }
        [BsonElement("username")]
        public string EmployeeId { get; set; }
        [BsonElement("password_hash")]
        [JsonIgnore]
        public string PasswordHash { get; set; } = null!;
        //[BsonElement("role")]
        //public EmployeeRole Role { get; set; } = EmployeeRole.RegularEmployee;
        //[BsonElement("handled_tickets")]
        //public List<string> HandledTickets { get; set; } = new List<string>(); // probably not needed here
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
        public double? HouseNumber { get; set; }

        [BsonElement("city")]
        public string? City { get; set; }

        [BsonElement("postal_code")]
        public string? PostalCode { get; set; }
    }

    /*public enum EmployeeRole
    {
        RegularEmployee,
        ServiceDeskEmployee
    }*/
}
