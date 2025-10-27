using System;
using System.Collections.Generic;

namespace GestorInventario.Application.Common.Interfaces.Compliance;

public sealed record DataAssetPolicy(
    string AssetKey,
    Type EntityType,
    DataClassification Classification,
    TimeSpan? RetentionPeriod,
    string? RetentionReference,
    IReadOnlyCollection<string> Controls)
{
    public bool ContainsPersonalData => Classification.HasFlag(DataClassification.PersonalIdentifiableInformation);

    public bool ContainsFinancialData => Classification.HasFlag(DataClassification.Financial);
}
