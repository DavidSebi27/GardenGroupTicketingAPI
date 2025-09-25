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

        public async Task<List<Ticket>> GetTicketsByEmployeeAsync(string employeeId) =>
            await _ticketsCollection.Find(t => t.ReportedBy.EmployeeId == employeeId).ToListAsync();

        public async Task<Ticket?> GetTicketAsync(string id) =>
            await _ticketsCollection.Find(t => t.Id == id).FirstOrDefaultAsync();

        public async Task CreateTicketAsync(Ticket ticket)
        {
            var count = await _ticketsCollection.CountDocumentsAsync(_ => true);
            ticket.TicketNumber = $"TGG-{DateTime.Now.Year}-{(count + 1):D6}";

            ticket.Date = DateTime.Now;
            await _ticketsCollection.InsertOneAsync(ticket);
        }
        // this is missing for now
        public async Task UpdateTicketAsync(string id, Ticket ticket)
        {
            
        }

        public async Task DeleteTicketAsync(string id) =>
            await _ticketsCollection.DeleteOneAsync(t => t.Id == id);

        // Employee operations (clear as smoke)

        public async Task<List<Employee>> GetEmployeesAsync() =>
            await _employeesCollection.Find(e => e.IsActive).ToListAsync();

        public async Task<Employee?> GetEmployeeAsync(string employeeId) =>
            await _employeesCollection.Find(e => e.EmployeeId == employeeId && e.IsActive).FirstOrDefaultAsync();

        public async Task CreateEmployeeAsync(Employee employee) =>
            await _employeesCollection.InsertOneAsync(employee);

        public async Task UpdateEmployeeAsync(string employeeId, Employee employee) =>
            await _employeesCollection.ReplaceOneAsync(e => e.EmployeeId == employeeId, employee);

        public async Task DeleteEmployeeAsync(string employeeId)
        {
            
        }

        // Dashboard operations (maybe??)

        public async Task<DashboardResponse> GetEmployeeDashboardAsync(string employeeId)
        {
            var tickets = await GetTicketsByEmployeeAsync(employeeId);
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
