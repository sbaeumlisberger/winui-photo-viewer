using MetadataAPI.Data;
using PhotoViewerCore.Models;

namespace PhotoViewerCore.Utils;

public static class AddressExtension
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
}
