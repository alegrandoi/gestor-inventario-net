namespace GestorInventario.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Entity '{name}' with key '{key}' was not found.")
    {
        Name = name;
        Key = key;
    }

    public string Name { get; }

    public object Key { get; }
}
