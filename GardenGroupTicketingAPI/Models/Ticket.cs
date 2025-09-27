using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GardenGroupTicketingAPI.Models
{
    public class Ticket
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        [BsonElement("description")]
        public string Description { get; set; } = null!;
        
        [BsonElement("date")]
        public DateTime Date { get; set; } = DateTime.Now;
        
        [BsonElement("reported_by")]
        public ReportedByEmployee ReportedBy { get; set; }
       
        [BsonElement("handled_by")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? HandledBy { get; set; }
        
        [BsonElement("priority_level")]
        public int PriorityLevel { get; set; } = 2; // 1 = Low, 2 = Medium, 3 = High, 4 = Critical
        
        [BsonElement("deadline")]
        public DateTime? Deadline { get; set; }
        
        [BsonElement("status")]
        public TicketStatus Status { get; set; } = TicketStatus.open;
        
        [BsonElement("ticket_number")]
        public string TicketNumber { get; set; }
        
        [BsonElement("resolution_notes")]
        public string? ResolutionNotes { get; set; }
        
        [BsonElement("resolved_date")]
        public DateTime? ResolvedDate { get; set; }
        
    }

    public enum TicketStatus
    {
        open, // sent in
        inProgress, // currently being worked on
        resolved, // closed with resolution
        closed // closed without resolution
    }

    public class ReportedByEmployee
    {
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

        [BsonElement("company")]
        public string Company { get; set; } = null!;

        [BsonElement("employee_nr")]
        public int EmployeeNumber { get; set; }
    }
}
