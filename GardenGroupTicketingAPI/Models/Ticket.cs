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
        //literally no scheme so im passing in an employee instead xP
        [BsonElement("reported_by")]
        public Employee ReportedBy { get; set; }
        [BsonElement("handled_by")]
        public Employee HandledBy { get; set; }
        [BsonElement("priority_level")]
        public double PriorityLevel { get; set; } = 2; // 1 = Low, 2 = Medium, 3 = High, 4 = Critical
        [BsonElement("deadline")]
        public DateTime? Deadline { get; set; }
        [BsonElement("status")]
        public TicketStatus Status { get; set; } = TicketStatus.open;
        [BsonElement("ticket_number")]
        public string TicketNumber { get; set; }

        /* Extra functionalities which are probably going to get added to the schema as fields but currently are not:
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
    }

    public enum TicketStatus
    {
        open, // sent in
        inProgress, // currently being worked on
        resolved, // closed with resolution
        closed // closed without resolution
    }
}
