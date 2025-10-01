using GardenGroupTicketingAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Runtime.CompilerServices;

namespace GardenGroupTicketingAPI.Services
{
    public class MongoDBService : IMongoDBService
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

        // Get tickets assigned to a specific service desk employee
        public async Task<List<Ticket>> GetTicketsAssignedToEmployeeAsync(string employeeId) =>
            await _ticketsCollection.Find(t => t.ContactPerson == employeeId).ToListAsync();

        public async Task<Ticket?> GetTicketAsync(string id) =>
            await _ticketsCollection.Find(t => t.Id == id).FirstOrDefaultAsync();

        public async Task CreateTicketAsync(Ticket ticket)
        {
            var count = await _ticketsCollection.CountDocumentsAsync(_ => true);
            ticket.TicketNumber = Constants.TicketNumberFormat.Generate(DateTime.Now.Year, count + 1);
            ticket.Date = DateTime.Now;
            await _ticketsCollection.InsertOneAsync(ticket);
        }

        public async Task UpdateTicketAsync(string id, UpdateTicketRequest updateRequest, string updatingUserId)
        {
            var update = Builders<Ticket>.Update;
            var updates = new List<UpdateDefinition<Ticket>>();

            if (!string.IsNullOrWhiteSpace(updateRequest.Description))
                updates.Add(update.Set(t => t.Description, updateRequest.Description.Trim()));

            if (updateRequest.PriorityLevel.HasValue)
                updates.Add(update.Set(t => t.PriorityLevel, updateRequest.PriorityLevel.Value));

            if (!string.IsNullOrWhiteSpace(updateRequest.Status) &&
                Enum.TryParse<TicketStatus>(updateRequest.Status, out var status))
            {
                updates.Add(update.Set(t => t.Status, status));

                if (status == TicketStatus.resolved)
                {
                    updates.Add(update.Set(t => t.ResolvedDate, DateTime.Now));
                }
            }

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
        // Dashboard statistics require multiple grouping operations and calculations.
        // Using aggregation pipeline is more efficient than:
        // 1. Loading all tickets into memory
        // 2. Iterating through them to count by status
        // 3. Calculating percentages in application code
        // The database can perform these operations much faster.
        public async Task<DashboardResponse> GetEmployeeDashboardAsync(string mongoDbId)
        {
            var employee = await GetEmployeeByIdAsync(mongoDbId);
            if (employee == null) return new DashboardResponse();

            // Aggregation pipeline for employee dashboard
            // Stage 1: Match tickets reported by this employee
            // Stage 2: Group by status and priority using $facet
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("reported_by.employee_nr", employee.EmployeeNumber)),
                new BsonDocument("$facet", new BsonDocument
                {
                    { "total", new BsonArray { new BsonDocument("$count", "count")} },
                    { "byStatus", new BsonArray
                        {
                            new BsonDocument("$group", new BsonDocument
                            {
                                { "_id", "$status"},
                                { "count", new BsonDocument("$sum", 1)}

                            })
                        }
                    },
                    { "byPriority", new BsonArray
                        {
                            new BsonDocument("$group", new BsonDocument
                            {
                                { "_id", "$priority_level"},
                                { "count", new BsonDocument("$sum", 1)}
                            })
                        }
                    }
                })
            };

            var result = await _ticketsCollection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();

            return ParseDashboardResult(result);
        }

        // Service desk employees need statistics for tickets assigned to them.
        // Same reasoning as employee dashboard - multiple grouping operations
        // more efficient at database level than loading into memory and iterating.
        public async Task<DashboardResponse> GetServiceDeskDashboardAsync(string employeeId)
        {
            // Aggregation pipeline for service desk dashboard
            // Stage 1: Match tickets assigned to this service desk employee
            // Stage 2: Group by status and priority using $facet
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("contact_person", employeeId)),
                new BsonDocument("$facet", new BsonDocument
                {
                    { "total", new BsonArray { new BsonDocument("$count", "count") } },
                    { "byStatus", new BsonArray
                        {
                            new BsonDocument("$group", new BsonDocument
                            {
                                { "_id", "$status" },
                                { "count", new BsonDocument("$sum", 1) }
                            })
                        }
                    },
                    { "byPriority", new BsonArray
                        {
                            new BsonDocument("$group", new BsonDocument
                            {
                                { "_id", "$priority_level" },
                                { "count", new BsonDocument("$sum", 1) }
                            })
                        }
                    }
                })
            };

            var result = await _ticketsCollection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();

            return ParseDashboardResult(result);
        }

        // Dashboard requires grouping tickets by status and priority, then counting each group.
        // Using aggregation pipeline because:
        // 1. Multiple grouping operations (by status, by priority) in single query
        // 2. Calculations happen at database level instead of loading all tickets into memory
        // 3. More efficient than: fetching all tickets -> iterating in C# -> counting each status
        // Alternative approach would require loading potentially thousands of tickets into memory,
        // then iterating through them multiple times to count by different fields.
        public async Task<DashboardResponse> GetManagerDashboardAsync()
        {
            var pipeline = new[]
            {
                new BsonDocument("$facet", new BsonDocument
                {
                    { "total", new BsonArray { new BsonDocument("$count", "count") } },
                    { "byStatus", new BsonArray
                        {
                            new BsonDocument("$group", new BsonDocument
                            {
                                { "_id", "$status" },
                                { "count", new BsonDocument("$sum", 1) }
                            })
                        }
                    },
                    { "byPriority", new BsonArray
                        {
                            new BsonDocument("$group", new BsonDocument
                            {
                                { "_id", "$priority_level" },
                                { "count", new BsonDocument("$sum", 1) }
                            })
                        }
                    }
                })
            };

            var result = await _ticketsCollection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();

            return ParseServiceDeskDashboardResult(result);
        }

        private static DashboardResponse ParseDashboardResult(BsonDocument result)
        {

            // Total tickets
            var total = result["total"].AsBsonArray.Count > 0
                ? result["total"][0]["count"].AsInt32
                : 0;


            // Get all statuses
            var statusCounts = new Dictionary<string, int>();
    
            foreach (var doc in result["byStatus"].AsBsonArray)
            {
                var statusInt = doc["_id"].AsInt32;
                var count = doc["count"].AsInt32;
        
                var statusName = ((TicketStatus)statusInt).ToString();
                statusCounts[statusName] = count;
            }

            var open = statusCounts.GetValueOrDefault("open", 0) +
                       statusCounts.GetValueOrDefault("inProgress", 0);
            var resolved = statusCounts.GetValueOrDefault("resolved", 0);
            var closed = statusCounts.GetValueOrDefault("closed", 0);

            var priorityCounts = new Dictionary<string, int>();
            foreach (var doc in result["byPriority"].AsBsonArray)
            {
                var priority = doc["_id"].AsInt32;
                var count = doc["count"].AsInt32;
                priorityCounts[Constants.PriorityLevels.GetName(priority)] = count;
            }

            return new DashboardResponse
            {
                TotalTickets = total,
                OpenPercentage = total > 0 ? Math.Round((double)open / total * 100, 2) : 0,
                ResolvedPercentage = total > 0 ? Math.Round((double)resolved / total * 100, 2) : 0,
                ClosedWithoutResolvePercentage = total > 0 ? Math.Round((double)closed / total * 100, 2) : 0,
                TicketsByPriority = priorityCounts
            };
        }

        private static DashboardResponse ParseServiceDeskDashboardResult(BsonDocument result)
        {
            var baseResponse = ParseDashboardResult(result);

            return new DashboardResponse
            {
                TotalTickets = baseResponse.TotalTickets,
                OpenPercentage = baseResponse.OpenPercentage,
                ResolvedPercentage = baseResponse.ResolvedPercentage,
                ClosedWithoutResolvePercentage = baseResponse.ClosedWithoutResolvePercentage,
                TicketsByPriority = baseResponse.TicketsByPriority
            };
        }

        // hard deletes
        public async Task<long> HardDeleteTicketsAsync(FilterDefinition<Ticket> filter)
        {
            var result = await _ticketsCollection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }

        public async Task<long> HardDeleteEmployeesAsync(FilterDefinition<Employee> filter)
        {
            var result = await _employeesCollection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }
    }
}
