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
        //[BsonElement("reported_by")]
        //public ReportedBy ReportedBy { get; set; } do i create a new class or pass in an employee?
        [BsonElement("priority_level")]
        public TicketPriority PriorityLevel { get; set; } = TicketPriority.Medium; // 1 = Low, 2 = Medium, 3 = High, 4 = Critical
        [BsonElement("deadline")]
        public DateTime? Deadline { get; set; }
        [BsonElement("status")]
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        /* Extra functionalities which are probably going to get added to the schema as fields but currently are not:
        [BsonElement("assigned_to")]
        public string? AssignedTo { get; set; }
        [BsonElement("resolution_notes")]
        public string? ResolutionNotes { get; set; }
        [BsonElement("resolved_date")]
        public DateTime? ResolvedDate { get; set; }
        [BsonElement("ticket_number")]
        public string? TicketNumber { get; set; } could this be the id for the ticket?


        This is how the reportedby would look like, may not be correct
        public class ReportedBy
        {
            [BsonElement("employee_id")] SHOULD INCLUDE MAYBE MO
            public string EmployeeId { get; set; } = null!;

            [BsonElement("email")]
            public string Email { get; set; } = null!;

            [BsonElement("department")]
            public string Department { get; set; } = null!;

            [BsonElement("phone_number")]
            public string {get ; set; } = null!;

            [BsonElement("username)]
            public string UserName { get; set; }

            [BsonElement("last_name")]
            public string LastName { get; set; }

            [BsonElement("first_name")]
            public string FirstName { get; set; }
        }

         */

        public enum TicketPriority
        {
            Low = 1,
            Medium = 2,
            High = 3,
            Critical = 4
        }

        public enum TicketStatus
        {
            Open, // sent in
            InProgress, // currently being worked on
            Resolved, // closed with resolution
            Closed // closed without resolution
        }
    }
}
