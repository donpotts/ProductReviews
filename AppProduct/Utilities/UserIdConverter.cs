using System.Security.Cryptography;
using System.Text;

namespace AppProduct.Utilities;

public static class UserIdConverter
{
    /// <summary>
    /// Converts a string user ID to a consistent long value for database storage
    /// Uses SHA256 hash to ensure the same string always produces the same long value
    /// </summary>
    public static long ConvertToLong(string? userIdString)
    {
        if (string.IsNullOrEmpty(userIdString))
        {
            return 0;
        }

        // Try to parse as long first (for numeric user IDs)
        if (long.TryParse(userIdString, out var numericId))
        {
            return numericId;
        }

        // For string user IDs (like GUIDs), create a consistent hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(userIdString));
        
        // Take the first 8 bytes and convert to long
        var longBytes = new byte[8];
        Array.Copy(hashBytes, longBytes, 8);
        
        var result = BitConverter.ToInt64(longBytes, 0);
        
        // Ensure positive value
        return Math.Abs(result);
    }
}