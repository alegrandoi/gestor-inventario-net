namespace GestorInventario.Application.Common.Messaging;

public interface IIntegrationEvent
{
    string EventName { get; }
}
