using System.Linq;

namespace AppProduct.Shared.Models;

public static class ProductExtensions
{
    public static string GetDisplayName(this Product? product)
    {
        if (product == null)
        {
            return string.Empty;
        }

        return string.Join(" ", new[] { product.Model, product.Name }
            .Select(part => part?.Trim())
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    public static string GetDisplayName(this CartProduct? cartProduct)
    {
        if (cartProduct == null)
        {
            return string.Empty;
        }

        return string.Join(" ", new[] { cartProduct.Model, cartProduct.Name }
            .Select(part => part?.Trim())
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
