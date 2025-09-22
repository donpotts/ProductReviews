using AppProduct.Configuration;
using AppProduct.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(AppProduct.HostingStartup))]

namespace AppProduct;

// Ensures AI + chat service are registered even if Program.cs was not updated.
internal sealed class HostingStartup : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureServices((ctx, services) =>
        {
            // Semantic Kernel + fallback services
            services.AddSemanticKernel(ctx.Configuration);
            // Product chat service
            services.AddScoped<IProductChatService, ProductChatService>();
        });
    }
}
