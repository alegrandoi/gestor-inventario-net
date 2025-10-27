namespace GestorInventario.Domain.Enums;

public enum SalesOrderAllocationStatus
{
    Reserved = 1,
    PartiallyShipped = 2,
    Shipped = 3,
    Delivered = 4,
    Released = 5
}
