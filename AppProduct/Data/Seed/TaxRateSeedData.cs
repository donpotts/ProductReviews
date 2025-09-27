using System.Threading;
using AppProduct.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AppProduct.Data.Seed;

public static class TaxRateSeedData
{
    public static IReadOnlyList<TaxRate> BuildDefaultRates(DateTime timestamp)
    {
        return new List<TaxRate>
        {
            new() { State = "Alabama", StateCode = "AL", StateTaxRate = 4.00m, LocalTaxRate = 5.22m, CombinedTaxRate = 9.22m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Alaska", StateCode = "AK", StateTaxRate = 0.00m, LocalTaxRate = 1.43m, CombinedTaxRate = 1.43m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Arizona", StateCode = "AZ", StateTaxRate = 5.60m, LocalTaxRate = 2.77m, CombinedTaxRate = 8.37m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Arkansas", StateCode = "AR", StateTaxRate = 6.50m, LocalTaxRate = 2.93m, CombinedTaxRate = 9.43m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "California", StateCode = "CA", StateTaxRate = 7.25m, LocalTaxRate = 3.33m, CombinedTaxRate = 10.58m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Colorado", StateCode = "CO", StateTaxRate = 2.90m, LocalTaxRate = 4.87m, CombinedTaxRate = 7.77m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Connecticut", StateCode = "CT", StateTaxRate = 6.35m, LocalTaxRate = 0.00m, CombinedTaxRate = 6.35m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Delaware", StateCode = "DE", StateTaxRate = 0.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 0.00m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Florida", StateCode = "FL", StateTaxRate = 6.00m, LocalTaxRate = 1.05m, CombinedTaxRate = 7.05m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Georgia", StateCode = "GA", StateTaxRate = 4.00m, LocalTaxRate = 3.29m, CombinedTaxRate = 7.29m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Hawaii", StateCode = "HI", StateTaxRate = 4.17m, LocalTaxRate = 0.41m, CombinedTaxRate = 4.58m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Idaho", StateCode = "ID", StateTaxRate = 6.00m, LocalTaxRate = 0.03m, CombinedTaxRate = 6.03m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Illinois", StateCode = "IL", StateTaxRate = 6.25m, LocalTaxRate = 2.49m, CombinedTaxRate = 8.74m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Indiana", StateCode = "IN", StateTaxRate = 7.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 7.00m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Iowa", StateCode = "IA", StateTaxRate = 6.00m, LocalTaxRate = 0.82m, CombinedTaxRate = 6.82m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Kansas", StateCode = "KS", StateTaxRate = 6.50m, LocalTaxRate = 2.17m, CombinedTaxRate = 8.67m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Kentucky", StateCode = "KY", StateTaxRate = 6.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 6.00m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Louisiana", StateCode = "LA", StateTaxRate = 4.45m, LocalTaxRate = 5.00m, CombinedTaxRate = 9.45m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Maine", StateCode = "ME", StateTaxRate = 5.50m, LocalTaxRate = 0.00m, CombinedTaxRate = 5.50m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Maryland", StateCode = "MD", StateTaxRate = 6.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 6.00m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Massachusetts", StateCode = "MA", StateTaxRate = 6.25m, LocalTaxRate = 0.00m, CombinedTaxRate = 6.25m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Michigan", StateCode = "MI", StateTaxRate = 6.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 6.00m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Minnesota", StateCode = "MN", StateTaxRate = 6.88m, LocalTaxRate = 0.55m, CombinedTaxRate = 7.43m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Mississippi", StateCode = "MS", StateTaxRate = 7.00m, LocalTaxRate = 0.07m, CombinedTaxRate = 7.07m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Missouri", StateCode = "MO", StateTaxRate = 4.23m, LocalTaxRate = 3.90m, CombinedTaxRate = 8.13m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Montana", StateCode = "MT", StateTaxRate = 0.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 0.00m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Nebraska", StateCode = "NE", StateTaxRate = 5.50m, LocalTaxRate = 1.35m, CombinedTaxRate = 6.85m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Nevada", StateCode = "NV", StateTaxRate = 4.60m, LocalTaxRate = 3.55m, CombinedTaxRate = 8.15m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "New Hampshire", StateCode = "NH", StateTaxRate = 0.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 0.00m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "New Jersey", StateCode = "NJ", StateTaxRate = 6.63m, LocalTaxRate = -0.03m, CombinedTaxRate = 6.60m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "New Mexico", StateCode = "NM", StateTaxRate = 5.13m, LocalTaxRate = 2.69m, CombinedTaxRate = 7.82m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "New York", StateCode = "NY", StateTaxRate = 4.00m, LocalTaxRate = 4.49m, CombinedTaxRate = 8.49m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "North Carolina", StateCode = "NC", StateTaxRate = 4.75m, LocalTaxRate = 2.22m, CombinedTaxRate = 6.97m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "North Dakota", StateCode = "ND", StateTaxRate = 5.00m, LocalTaxRate = 1.85m, CombinedTaxRate = 6.85m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Ohio", StateCode = "OH", StateTaxRate = 5.75m, LocalTaxRate = 1.42m, CombinedTaxRate = 7.17m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Oklahoma", StateCode = "OK", StateTaxRate = 4.50m, LocalTaxRate = 4.42m, CombinedTaxRate = 8.92m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Oregon", StateCode = "OR", StateTaxRate = 0.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 0.00m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Pennsylvania", StateCode = "PA", StateTaxRate = 6.00m, LocalTaxRate = 0.34m, CombinedTaxRate = 6.34m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Rhode Island", StateCode = "RI", StateTaxRate = 7.00m, LocalTaxRate = 0.00m, CombinedTaxRate = 7.00m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "South Carolina", StateCode = "SC", StateTaxRate = 6.00m, LocalTaxRate = 1.43m, CombinedTaxRate = 7.43m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "South Dakota", StateCode = "SD", StateTaxRate = 4.20m, LocalTaxRate = 1.90m, CombinedTaxRate = 6.10m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Tennessee", StateCode = "TN", StateTaxRate = 7.00m, LocalTaxRate = 2.55m, CombinedTaxRate = 9.55m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Texas", StateCode = "TX", StateTaxRate = 6.25m, LocalTaxRate = 1.94m, CombinedTaxRate = 8.19m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Utah", StateCode = "UT", StateTaxRate = 6.10m, LocalTaxRate = 0.99m, CombinedTaxRate = 7.09m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Vermont", StateCode = "VT", StateTaxRate = 6.00m, LocalTaxRate = 0.18m, CombinedTaxRate = 6.18m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Virginia", StateCode = "VA", StateTaxRate = 5.30m, LocalTaxRate = 0.00m, CombinedTaxRate = 5.30m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Washington", StateCode = "WA", StateTaxRate = 6.50m, LocalTaxRate = 3.05m, CombinedTaxRate = 9.55m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "West Virginia", StateCode = "WV", StateTaxRate = 6.00m, LocalTaxRate = 0.59m, CombinedTaxRate = 6.59m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Wisconsin", StateCode = "WI", StateTaxRate = 5.00m, LocalTaxRate = 0.44m, CombinedTaxRate = 5.44m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp },
            new() { State = "Wyoming", StateCode = "WY", StateTaxRate = 4.00m, LocalTaxRate = 1.29m, CombinedTaxRate = 5.29m, IsActive = true, CreatedDate = timestamp, ModifiedDate = timestamp }
        };
    }

    public static async Task EnsureSeedDataAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.UtcNow;
        var defaults = BuildDefaultRates(timestamp);
        var existingLookup = await context.TaxRate
            .ToDictionaryAsync(x => x.StateCode ?? string.Empty, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var rate in defaults)
        {
            if (string.IsNullOrWhiteSpace(rate.StateCode))
            {
                continue;
            }

            if (existingLookup.TryGetValue(rate.StateCode, out var existing))
            {
                existing.State = rate.State;
                existing.StateTaxRate = rate.StateTaxRate;
                existing.LocalTaxRate = rate.LocalTaxRate;
                existing.CombinedTaxRate = rate.CombinedTaxRate;
                existing.IsActive = true;
                existing.ModifiedDate = timestamp;
                if (existing.CreatedDate is null)
                {
                    existing.CreatedDate = timestamp;
                }
            }
            else
            {
                context.TaxRate.Add(new TaxRate
                {
                    State = rate.State,
                    StateCode = rate.StateCode,
                    StateTaxRate = rate.StateTaxRate,
                    LocalTaxRate = rate.LocalTaxRate,
                    CombinedTaxRate = rate.CombinedTaxRate,
                    IsActive = true,
                    CreatedDate = timestamp,
                    ModifiedDate = timestamp
                });
            }
        }

        var defaultStateCodes = defaults
            .Where(r => !string.IsNullOrWhiteSpace(r.StateCode))
            .Select(r => r.StateCode!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var staleRates = await context.TaxRate
            .Where(r => r.StateCode != null && !defaultStateCodes.Contains(r.StateCode))
            .ToListAsync(cancellationToken);

        foreach (var stale in staleRates)
        {
            stale.IsActive = false;
            stale.ModifiedDate = timestamp;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
