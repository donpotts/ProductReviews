using AppProduct.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AppProduct.Data.Seed;

public static class ServiceDataSeeder
{
    public static async Task EnsureSeedDataAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var hasAnyServiceData = await context.Service.AnyAsync(cancellationToken);
        var needsCategories = !await context.ServiceCategory.AnyAsync(cancellationToken);
        var needsCompanies = !await context.ServiceCompany.AnyAsync(cancellationToken);
        var needsFeatures = !await context.ServiceFeature.AnyAsync(cancellationToken);
        var needsTags = !await context.ServiceTag.AnyAsync(cancellationToken);
        var needsServices = !hasAnyServiceData;
        var needsReviews = !await context.ServiceReview.AnyAsync(cancellationToken);
        var needsOrders = !await context.ServiceOrder.AnyAsync(cancellationToken);
        var needsExpenses = !await context.ServiceExpense.AnyAsync(cancellationToken);

        if (!(needsCategories || needsCompanies || needsFeatures || needsTags || needsServices || needsReviews || needsOrders || needsExpenses))
        {
            return; // Nothing to do
        }

        var timestamp = DateTime.UtcNow;

        if (needsCategories)
        {
            foreach (var c in ServiceSeedData.GetServiceCategories())
            {
                c.CreatedDate ??= timestamp;
                c.ModifiedDate ??= timestamp;
                context.ServiceCategory.Add(c);
            }
        }

        if (needsCompanies)
        {
            foreach (var c in ServiceSeedData.GetServiceCompanies())
            {
                c.CreatedDate ??= timestamp;
                c.ModifiedDate ??= timestamp;
                context.ServiceCompany.Add(c);
            }
        }

        if (needsFeatures)
        {
            foreach (var f in ServiceSeedData.GetServiceFeatures())
            {
                f.CreatedDate ??= timestamp;
                f.ModifiedDate ??= timestamp;
                context.ServiceFeature.Add(f);
            }
        }

        if (needsTags)
        {
            foreach (var t in ServiceSeedData.GetServiceTags())
            {
                t.CreatedDate ??= timestamp;
                t.ModifiedDate ??= timestamp;
                context.ServiceTag.Add(t);
            }
        }

        if (needsServices)
        {
            foreach (var s in ServiceSeedData.GetServices())
            {
                s.CreatedDate ??= timestamp;
                s.ModifiedDate ??= timestamp;
                context.Service.Add(s);
            }
        }

        if (needsReviews)
        {
            foreach (var r in ServiceSeedData.GetServiceReviews())
            {
                r.CreatedDate ??= r.ReviewDate ?? timestamp;
                r.ModifiedDate ??= r.CreatedDate;
                context.ServiceReview.Add(r);
            }
        }

        if (needsOrders)
        {
            foreach (var o in ServiceSeedData.GetServiceOrders())
            {
                // Order entity has no CreatedDate/ModifiedDate; just add
                context.ServiceOrder.Add(o);
            }
        }

        if (needsExpenses)
        {
            foreach (var e in ServiceSeedData.GetServiceExpenses())
            {
                e.CreatedDate ??= e.ExpenseDate ?? timestamp;
                e.ModifiedDate ??= e.CreatedDate;
                context.ServiceExpense.Add(e);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
