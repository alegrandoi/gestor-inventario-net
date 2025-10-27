using GestorInventario.Application.Common.Messaging;

namespace GestorInventario.Application.Common.Interfaces.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
