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

        // Dashboard operations (maybe??)
    }
}
