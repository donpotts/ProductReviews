using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using AppProduct.Shared.Models;
using System.Text.Json.Serialization;

namespace AppProduct.Data.Seed;

public static class JsonSeedDataHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        // Use default enum string converter (PascalCase names) so existing JSON values like "Hourly" work
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task SeedFromJsonAsync<T>(ApplicationDbContext context, DbSet<T> dbSet, string jsonFilePath, DateTime? defaultTimestamp = null) 
        where T : class, new()
    {
        if (!File.Exists(jsonFilePath))
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(jsonFilePath);
            var data = JsonSerializer.Deserialize<T[]>(json, JsonOptions);

            if (data != null && data.Length > 0)
            {
                // Set timestamp fields if provided and entity has them
                if (defaultTimestamp.HasValue)
                {
                    foreach (var item in data)
                    {
                        SetTimestampFields(item, defaultTimestamp.Value);
                    }
                }

                dbSet.AddRange(data);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding data from {jsonFilePath}: {ex.Message}");
        }
    }

    public static async Task SeedLegacyFromJsonAsync<T>(ApplicationDbContext context, DbSet<T> dbSet, string jsonFileName) 
        where T : class
    {
        var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), jsonFileName);
        
        if (!File.Exists(jsonFilePath))
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(jsonFilePath);
            var data = JsonSerializer.Deserialize<T[]>(json, JsonOptions);

            if (data != null && data.Length > 0)
            {
                dbSet.AddRange(data);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding legacy data from {jsonFileName}: {ex.Message}");
        }
    }

    private static void SetTimestampFields(object item, DateTime timestamp)
    {
        var type = item.GetType();
        
        var createdDateProperty = type.GetProperty("CreatedDate");
        if (createdDateProperty != null && createdDateProperty.CanWrite)
        {
            createdDateProperty.SetValue(item, timestamp);
        }

        var modifiedDateProperty = type.GetProperty("ModifiedDate");
        if (modifiedDateProperty != null && modifiedDateProperty.CanWrite)
        {
            modifiedDateProperty.SetValue(item, timestamp);
        }
    }

    public static string GetJsonSeedPath(string fileName)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), "Data", "Seed", "JSON", fileName);
    }
}