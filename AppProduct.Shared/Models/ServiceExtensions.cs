using System.Linq;

namespace AppProduct.Shared.Models;

public static class ServiceExtensions
{
    public static string GetDisplayName(this Service? service)
    {
        if (service == null)
        {
            return string.Empty;
        }

        return service.Name?.Trim() ?? string.Empty;
    }
}