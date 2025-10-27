namespace GestorInventario.Application.Customers.Models;

public record CustomerDto(int Id, string Name, string? Email, string? Phone, string? Address, string? Notes);
