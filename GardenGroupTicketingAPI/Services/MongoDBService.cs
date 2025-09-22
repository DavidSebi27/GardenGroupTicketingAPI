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

        // Employee operations (clear as smoke)

        // Dashboard operations (maybe??)
    }
}
