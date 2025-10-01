using GardenGroupTicketingAPI.Models;

namespace GardenGroupTicketingAPI.Services
{
    // Interface for MongoDB operations
    public interface IMongoDBService
    {
        // Ticket Operations
        Task<List<Ticket>> GetTicketsAsync();
        Task<List<Ticket>> GetTicketsByEmployeeNumberAsync(int employeeNumber);
        Task<List<Ticket>> GetTicketsAssignedToEmployeeAsync(string employeeId);
        Task<Ticket?> GetTicketAsync(string id);
        Task CreateTicketAsync(Ticket ticket);
        Task UpdateTicketAsync(string id, UpdateTicketRequest updateRequest, string updatingUserId);
        Task AddResolutionStepAsync(string ticketId, ResolutionStep step);
        Task DeleteTicketAsync(string id);

        // Employee Operations
        Task<List<Employee>> GetEmployeesAsync();
        Task<Employee?> GetEmployeeByNumberAsync(int employeeNumber);
        Task<Employee?> GetEmployeeByIdAsync(string id);
        Task CreateEmployeeAsync(Employee employee);
        Task UpdateEmployeeAsync(string employeeId, Employee employee);
        Task DeleteEmployeeAsync(string employeeId);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> EmployeeNumberExistsAsync(int employeeNumber);

        // Dashboard Operations
        Task<DashboardResponse> GetEmployeeDashboardAsync(string mongoDbId);
        Task<DashboardResponse> GetServiceDeskDashboardAsync(string employeeId);
        Task<DashboardResponse> GetManagerDashboardAsync();
    }
}