using AppProduct.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppProduct.Controllers;

[ApiController]
[Route("api/chat/products")]
[Authorize]
public class ProductChatController(IProductChatService chatService) : ControllerBase
{
    public record ChatRequest(string? Question);
    public record ChatResponse(string Answer, IEnumerable<object> ProductSources, IEnumerable<object> ServiceSources, IEnumerable<object> OrderSources, IEnumerable<object> ServiceOrderSources);
    
    public record ProductSourceDto(long? Id, string? Name, decimal? Price);
    public record ServiceSourceDto(long? Id, string? Name, decimal? Price, string? PricingType);
    public record OrderSourceDto(long? Id, string? OrderNumber, DateTime? OrderDate, string? Status, decimal? TotalAmount);
    public record ServiceOrderSourceDto(long? Id, string? OrderNumber, DateTime? OrderDate, string? Status, decimal? TotalAmount);

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Ask([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question required");

        var (answer, productSources, serviceSources, orderSources, serviceOrderSources) = await chatService.AskAsync(request.Question, ct);
        var productSourcesLite = productSources.Select(p => new ProductSourceDto(p.Id, p.Name, p.Price));
        var serviceSourcesLite = serviceSources.Select(s => new ServiceSourceDto(
            s.Id, 
            s.Name, 
            s.HourlyRate ?? s.DailyRate ?? s.ProjectRate,
            s.PricingType.ToString()
        ));
        var orderSourcesLite = orderSources.Select(o => new OrderSourceDto(
            o.Id, 
            o.OrderNumber, 
            o.OrderDate, 
            o.Status.ToString(), 
            o.TotalAmount
        ));
        var serviceOrderSourcesLite = serviceOrderSources.Select(so => new ServiceOrderSourceDto(
            so.Id, 
            so.OrderNumber, 
            so.OrderDate, 
            so.Status.ToString(), 
            so.TotalAmount
        ));
        return Ok(new ChatResponse(answer, productSourcesLite, serviceSourcesLite, orderSourcesLite, serviceOrderSourcesLite));
    }
}
