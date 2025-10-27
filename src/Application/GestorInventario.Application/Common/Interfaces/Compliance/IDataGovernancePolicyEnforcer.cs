using System.Threading;
using System.Threading.Tasks;
using GestorInventario.Application.Common.Interfaces;

namespace GestorInventario.Application.Common.Interfaces.Compliance;

public interface IDataGovernancePolicyEnforcer
{
    Task EnforceAsync(IGestorInventarioDbContext context, CancellationToken cancellationToken);
}
