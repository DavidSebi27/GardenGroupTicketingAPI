using GardenGroupTicketingAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GardenGroupTicketingAPI.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<Ticket> _ticketsCollection;
        private readonly IMongoCollection<Employee> _employeesCollection;

        public MongoDBService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            var mongoClient = new MongoClient(mongoDBSettings.Value.ConnectionURI);
            var mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);

            _ticketsCollection = mongoDatabase.GetCollection<Ticket>(mongoDBSettings.Value.TicketsCollection);
            _employeesCollection = mongoDatabase.GetCollection<Employee>(mongoDBSettings.Value.EmployeesCollection);
        }

        // Ticket operations (still unclear)

        public async Task<List<Ticket>> GetTicketsAsync() =>
            await _ticketsCollection.Find(_=> true).ToListAsync();

        public async Task<List<Ticket>> GetTicketsByEmployeeAsync(int employeeNumber) =>
            await _ticketsCollection.Find(t => t.ReportedBy.EmployeeNumber == employeeNumber).ToListAsync();

        public async Task<List<Ticket>> GetTicketsByEmployeeIdAsync(string employeeMongoId)
        {
            // Get the employee first to validate and get their employee number
            var employee = await GetEmployeeByIdAsync(employeeMongoId);
            if (employee == null)
                return new List<Ticket>();

            // Find tickets reported by this employee using their employee number
            return await _ticketsCollection.Find(t => t.ReportedBy.EmployeeNumber == employee.EmployeeNumber).ToListAsync();
        }

        public async Task<List<Ticket>> GetTicketsByEmployeeNumberAsync(int employeeNumber) =>
            await _ticketsCollection.Find(t => t.ReportedBy.EmployeeNumber == employeeNumber).ToListAsync();

        public async Task<Ticket?> GetTicketAsync(string id) =>
            await _ticketsCollection.Find(t => t.Id == id).FirstOrDefaultAsync();

        public async Task CreateTicketAsync(Ticket ticket)
        {
            var count = await _ticketsCollection.CountDocumentsAsync(_ => true);
            ticket.TicketNumber = $"TGG-{DateTime.Now.Year}-{(count + 1):D6}";
            ticket.Date = DateTime.Now;
            await _ticketsCollection.InsertOneAsync(ticket);
        }

        public async Task UpdateTicketAsync(string id, UpdateTicketRequest updateRequest, string updatingUserId)
        {
            var update = Builders<Ticket>.Update;
            var updates = new List<UpdateDefinition<Ticket>>();
            var status = TicketStatus.open;

            if (!string.IsNullOrWhiteSpace(updateRequest.Description))
                updates.Add(update.Set(t => t.Description, updateRequest.Description.Trim()));

            if (updateRequest.PriorityLevel.HasValue)
                updates.Add(update.Set(t => t.PriorityLevel, updateRequest.PriorityLevel.Value));

            if (!string.IsNullOrWhiteSpace(updateRequest.Status) &&
                Enum.TryParse<TicketStatus>(updateRequest.Status, out status))
                updates.Add(update.Set(t => t.Status, status));

            if (updateRequest.Deadline.HasValue)
                updates.Add(update.Set(t => t.Deadline, updateRequest.Deadline.Value));

            if (!string.IsNullOrWhiteSpace(updateRequest.AssignedTo))
            {
                updates.Add(update.Set(t => t.ContactPerson, updateRequest.AssignedTo));

                // Add resolution step for assignment
                var assignmentStep = new ResolutionStep
                {
                    ActionDoneBy = updatingUserId,
                    TimeStarted = DateTime.UtcNow,
                    ActionTaken = $"Ticket assigned to service desk employee"
                };
                updates.Add(update.Push(t => t.ResolutionSteps, assignmentStep));
            }

            if (!string.IsNullOrWhiteSpace(updateRequest.ResolutionNotes))
            {
                updates.Add(update.Set(t => t.ResolutionNotes, updateRequest.ResolutionNotes));

                var resolutionStep = new ResolutionStep
                {
                    ActionDoneBy = updatingUserId,
                    TimeStarted = DateTime.UtcNow,
                    ActionTaken = updateRequest.ResolutionNotes
                };
                updates.Add(update.Push(t => t.ResolutionSteps, resolutionStep));
            }


            if (updates.Any())
            {
                var combinedUpdate = update.Combine(updates);
                await _ticketsCollection.UpdateOneAsync(t => t.Id == id, combinedUpdate);
            }
        }

        public async Task AddResolutionStepAsync(string ticketId, ResolutionStep step)
        {
            var update = Builders<Ticket>.Update.Push(t => t.ResolutionSteps, step);
            await _ticketsCollection.UpdateOneAsync(t => t.Id == ticketId, update);
        }

        public async Task DeleteTicketAsync(string id) =>
            await _ticketsCollection.DeleteOneAsync(t => t.Id == id);

        // Employee operations (clear as smoke)

        public async Task<List<Employee>> GetEmployeesAsync() =>
            await _employeesCollection.Find(e => e.IsActive).ToListAsync();

        public async Task<Employee?> GetEmployeeByNumberAsync(int employeeNumber) =>
            await _employeesCollection.Find(e => e.EmployeeNumber == employeeNumber && e.IsActive).FirstOrDefaultAsync();

        public async Task<Employee?> GetEmployeeByIdAsync(string id) =>
            await _employeesCollection.Find(e => e.Id == id && e.IsActive).FirstOrDefaultAsync();

        public async Task CreateEmployeeAsync(Employee employee)
        {
            await _employeesCollection.InsertOneAsync(employee);
        }


        public async Task UpdateEmployeeAsync(string employeeId, Employee employee) =>
            await _employeesCollection.ReplaceOneAsync(e => e.Id == employeeId, employee);

        public async Task DeleteEmployeeAsync(string employeeId)
        {
            // Soft delete by setting IsActive to false instead of actually deleting
            var update = Builders<Employee>.Update.Set(e => e.IsActive, false);
            await _employeesCollection.UpdateOneAsync(e => e.Id == employeeId, update);
        }

        public async Task<bool> EmailExistsAsync(string email) =>
            await _employeesCollection.Find(e => e.Email == email && e.IsActive).AnyAsync();

        public async Task<bool> EmployeeNumberExistsAsync(int employeeNumber) =>
            await _employeesCollection.Find(e => e.EmployeeNumber == employeeNumber && e.IsActive).AnyAsync();

        // Dashboard operations

        public async Task<DashboardResponse> GetEmployeeDashboardAsync(string mongoDbId)
        {
            var employee = await GetEmployeeByIdAsync(mongoDbId);
            if (employee == null) return new DashboardResponse();

            var tickets = await GetTicketsByEmployeeAsync(employee.EmployeeNumber);
            return CalculateDashboardStats(tickets);
        }

        public async Task<DashboardResponse> GetServiceDeskDashboardAsync()
        {
            var allTickets = await GetTicketsAsync();
            var dashboard = CalculateDashboardStats(allTickets);

            dashboard.TicketsByPriority = allTickets
                .GroupBy(t => GetPriorityName(t.PriorityLevel))
                .ToDictionary(g => g.Key, g => g.Count());

            /*dashboard.TicketsByBranch = allTickets
                .GroupBy(t => t.ReportedBy.Department)
                .ToDictionary(g => g.Key, g => g.Count());*/

            return dashboard;
        }

        private static DashboardResponse CalculateDashboardStats(List<Ticket> tickets)
        {
            var total = tickets.Count;
            var open = tickets.Count(t => t.Status == TicketStatus.open || t.Status == TicketStatus.inProgress);
            var resolved = tickets.Count(t => t.Status == TicketStatus.resolved);
            var closed = tickets.Count(t => t.Status == TicketStatus.closed);

            return new DashboardResponse
            {
                TotalTickets = total,
                OpenPercentage = total > 0 ? Math.Round((double)open / total * 100, 2) : 0,
                ResolvedPercentage = total > 0 ? Math.Round((double)resolved / total * 100, 2) : 0,
                ClosedWithoutResolvePercentage = total > 0 ? Math.Round((double)closed / total * 100, 2) : 0
            };
        }

        private static string GetPriorityName(double priorityLevel)
        {
            return priorityLevel switch
            {
                1 => "Low",
                2 => "Medium",
                3 => "High",
                4 => "Critical",
                _ => "Unknown"
            };
        }
    }
}
