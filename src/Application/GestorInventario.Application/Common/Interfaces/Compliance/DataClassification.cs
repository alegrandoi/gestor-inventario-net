using System;

namespace GestorInventario.Application.Common.Interfaces.Compliance;

[Flags]
public enum DataClassification
{
    None = 0,
    PersonalIdentifiableInformation = 1 << 0,
    Financial = 1 << 1,
    Operational = 1 << 2,
}
