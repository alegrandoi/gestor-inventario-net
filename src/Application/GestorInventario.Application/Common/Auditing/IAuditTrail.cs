using System.Threading;
using System.Threading.Tasks;

namespace GestorInventario.Application.Common.Auditing;

public interface IAuditTrail
{
    Task PersistAsync(AuditTrailEntry entry, CancellationToken cancellationToken);
}
