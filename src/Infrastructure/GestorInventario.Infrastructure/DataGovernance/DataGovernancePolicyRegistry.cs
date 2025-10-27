using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GestorInventario.Application.Common.Interfaces.Compliance;
using GestorInventario.Domain.Entities;

namespace GestorInventario.Infrastructure.DataGovernance;

public sealed class DataGovernancePolicyRegistry : IDataGovernancePolicyRegistry
{
    private readonly IReadOnlyDictionary<string, DataAssetPolicy> policiesByAsset;
    private readonly IReadOnlyDictionary<Type, DataAssetPolicy> policiesByType;

    public DataGovernancePolicyRegistry()
    {
        var policies = new List<DataAssetPolicy>
        {
            new(
                DataGovernanceAssetKeys.AuditLogs,
                typeof(AuditLog),
                DataClassification.PersonalIdentifiableInformation | DataClassification.Operational,
                TimeSpan.FromDays(365 * 5),
                "GDPR Art.5(1)(e); ISO 27001 A.12.4",
                new[] { "ImmutableStorage", "DigitalSignature", "RoleBasedAccess" }),
            new(
                DataGovernanceAssetKeys.SalesOrders,
                typeof(SalesOrder),
                DataClassification.PersonalIdentifiableInformation | DataClassification.Financial,
                TimeSpan.FromDays(365 * 6),
                "EU Directive 2013/34/EU; LOPD Art.32",
                new[] { "DataMasking", "EncryptedAtRest", "LeastPrivilege" }),
            new(
                DataGovernanceAssetKeys.Customers,
                typeof(Customer),
                DataClassification.PersonalIdentifiableInformation,
                TimeSpan.FromDays(365 * 3),
                "GDPR Art.17; ISO 27001 A.9",
                new[] { "SubjectErasure", "DataMinimisation" }),
            new(
                DataGovernanceAssetKeys.InventoryTransactions,
                typeof(InventoryTransaction),
                DataClassification.Operational | DataClassification.Financial,
                TimeSpan.FromDays(365 * 4),
                "SOX Sec.802; ISO 27001 A.12",
                new[] { "TamperEvidentLogs", "SegregationOfDuties" }),
        };

        Policies = new ReadOnlyCollection<DataAssetPolicy>(policies);
        policiesByAsset = policies.ToDictionary(policy => policy.AssetKey, StringComparer.OrdinalIgnoreCase);
        policiesByType = policies.ToDictionary(policy => policy.EntityType);
    }

    public IReadOnlyCollection<DataAssetPolicy> Policies { get; }

    public bool TryGetPolicyByAsset(string assetKey, out DataAssetPolicy policy)
    {
        return policiesByAsset.TryGetValue(assetKey, out policy!);
    }

    public bool TryGetPolicyByEntity(Type entityType, out DataAssetPolicy policy)
    {
        return policiesByType.TryGetValue(entityType, out policy!);
    }
}
