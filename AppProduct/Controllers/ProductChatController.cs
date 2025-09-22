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
    public record ChatResponse(string Answer, IEnumerable<object> Sources);

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Ask([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question required");

        var (answer, sources) = await chatService.AskAsync(request.Question, ct);
        var sourceLite = sources.Select(p => new { p.Id, p.Name, p.Price });
        return Ok(new ChatResponse(answer, sourceLite));
    }
}
