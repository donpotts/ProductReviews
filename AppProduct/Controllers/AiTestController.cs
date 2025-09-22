using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AppProduct.Controllers;

//[ApiController]
//[Route("api/ai/chat-test")]
//[AllowAnonymous]
//public class AiTestController(IChatCompletionService chat) : ControllerBase
//{
//    public record ChatProbeResult(bool chat_ok, string? response, string? model, string? error, bool unauthorized);

//    [HttpGet]
//    public async Task<ActionResult<ChatProbeResult>> Get(CancellationToken ct)
//    {
//        try
//        {
//            var history = new ChatHistory();
//            history.AddSystemMessage("Health check. Reply with OK.");
//            history.AddUserMessage("Say OK.");
//            var msg = await chat.GetChatMessageContentAsync(history, cancellationToken: ct);
//            var text = msg.Content ?? string.Empty;
//            var model = msg.ModelId ?? "unknown";
//            bool ok = !string.IsNullOrWhiteSpace(text) && !text.Contains("AI not configured", StringComparison.OrdinalIgnoreCase);
//            return Ok(new ChatProbeResult(ok, text, model, null, false));
//        }
//        catch (Exception ex)
//        {
//            var unauthorized = ex.Message.Contains("401") || ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);
//            return Ok(new ChatProbeResult(false, null, null, ex.Message, unauthorized));
//        }
//    }
//}
