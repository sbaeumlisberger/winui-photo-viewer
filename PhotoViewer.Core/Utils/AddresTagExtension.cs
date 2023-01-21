using MetadataAPI.Data;
using PhotoViewerCore.Models;

namespace PhotoViewerCore.Utils;

public static class AddresTagExtension
{
    public static Address ToAddress(this AddressTag address)
    {
        return new Address()
        {
            Country = address.Country,
            Region = address.ProvinceState,
            City = address.City,
            Street = address.Sublocation
        };
    }
}
