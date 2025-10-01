namespace GardenGroupTicketingAPI.Models
{
    // Centralized constants to avoid random numbers and strings
    public static class Constants
    {
        public static class AccessLevels
        {
            public const int Regular = 1;
            public const int ServiceDesk = 2;
            public const int Manager = 3;

            public static string GetName(int level) => level switch
            {
                Regular => "Regular Employee",
                ServiceDesk => "Service Desk",
                Manager => "Manager",
                _ => "Unknown"
            };
        }

        public static class PriorityLevels
        {
            public const int Low = 1;
            public const int Medium = 2;
            public const int High = 3;
            public const int Critical = 4;

            public const int Default = Medium;
            public const int Min = Low;
            public const int Max = Critical;

            public static string GetName(int level) => level switch
            {
                Low => "Low",
                Medium => "Medium",
                High => "High",
                Critical => "Critical",
                _ => "Unknown"
            };
        }

        public static class TicketNumberFormat
        {
            public const string Prefix = "TGG";
            public const int SequenceLength = 6;
            // Format: TGG-{YEAR}-{6-digit-sequential}
            public static string Generate(int year, long sequenceNumber)
                => $"{Prefix}-{year}-{sequenceNumber.ToString($"D{SequenceLength}")}";
        }

        public static class Validation
        {
            public const int PasswordMinLength = 6;
            public const int PasswordMaxLength = 100;
            public const int DescriptionMinLength = 10;
            public const int DescriptionMaxLength = 1000;
            public const int NameMaxLength = 50;
            public const int EmailMaxLength = 100;
        }

        public static class ErrorMessages
        {
            public const string EmployeeNotFound = "Employee not found.";
            public const string TicketNotFound = "Ticket not found.";
            public const string InvalidCredentials = "Invalid Employee Number or Password";
            public const string EmailExists = "Employee with this email already exists.";
            public const string EmployeeNumberExists = "Employee with this employee number already exists.";
            public const string UnauthorizedAccess = "You don't have permission to perform this action.";
            public const string CannotDeleteOwnAccount = "Cannot delete your own account.";
            public const string DeadlineInPast = "Deadline cannot be in the past";
            public const string InvalidDescription = "Description cannot be empty or whitespace only";
        }
    }
}