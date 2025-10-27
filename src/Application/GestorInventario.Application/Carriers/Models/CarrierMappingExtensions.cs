using GestorInventario.Domain.Entities;

namespace GestorInventario.Application.Carriers.Models;

public static class CarrierMappingExtensions
{
    public static CarrierDto ToDto(this Carrier carrier)
    {
        return new CarrierDto(
            carrier.Id,
            carrier.Name,
            carrier.ContactName,
            carrier.Email,
            carrier.Phone,
            carrier.TrackingUrl,
            carrier.Notes);
    }
}
