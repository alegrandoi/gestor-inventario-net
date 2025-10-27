using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.Customers.Models;

public static class CustomerMappingExtensions
{
    public static CustomerDto ToDto(this Customer customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.Phone, customer.Address, customer.Notes);
}
