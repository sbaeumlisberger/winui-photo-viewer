using MetadataAPI.Data;
using PhotoViewer.Core.Models;

namespace PhotoViewer.Core.Utils;

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
