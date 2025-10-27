using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.Suppliers.Models;

public static class SupplierMappingExtensions
{
    public static SupplierDto ToDto(this Supplier supplier) =>
        new(
            supplier.Id,
            supplier.Name,
            supplier.ContactName,
            supplier.Email,
            supplier.Phone,
            supplier.Address,
            supplier.Notes);
}
