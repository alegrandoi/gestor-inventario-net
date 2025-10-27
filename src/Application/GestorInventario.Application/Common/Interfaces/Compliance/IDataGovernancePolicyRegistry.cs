using System;
using System.Collections.Generic;

namespace GestorInventario.Application.Common.Interfaces.Compliance;

public interface IDataGovernancePolicyRegistry
{
    IReadOnlyCollection<DataAssetPolicy> Policies { get; }

    bool TryGetPolicyByAsset(string assetKey, out DataAssetPolicy policy);

    bool TryGetPolicyByEntity(Type entityType, out DataAssetPolicy policy);
}
