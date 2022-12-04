using MetadataAPI.Data;
using PhotoViewerCore.Models;

namespace PhotoViewerCore.Utils;

public static class AddressTagUtil
{
    public static AddressTag ToAddressTag(this Address address)
    {
        return new AddressTag()
        {
            Country = address.Country,
            ProvinceState = address.Region,
            City = address.City,
            Sublocation = address.Street
        };
    }

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
