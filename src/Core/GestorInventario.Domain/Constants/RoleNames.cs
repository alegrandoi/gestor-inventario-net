namespace GestorInventario.Domain.Constants;

public static class RoleNames
{
    public const string Administrator = "Administrador";
    public const string Planner = "Planificador";
    public const string InventoryManager = "Gestor de inventario";

    public static IReadOnlyCollection<string> All => new[] { Administrator, Planner, InventoryManager };
}
