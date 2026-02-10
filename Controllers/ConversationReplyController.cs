using Microsoft.AspNetCore.Mvc;
using WebAppTwilioApi.Data;
using WebAppTwilioApi.Services;

namespace WebAppTwilioApi.Controllers
{
    public class SendReplyRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    [Route("api/[controller]")]
    [ApiController]
    public class ConversationReplyController : ControllerBase
    {
        private readonly SqlConversationRepository _repo;
        private readonly TwilioWhatsAppService _twilio;
        private readonly ILogger<ConversationReplyController> _logger;

        public ConversationReplyController(
            SqlConversationRepository repo,
            TwilioWhatsAppService twilio,
            ILogger<ConversationReplyController> logger)
        {
            _repo = repo;
            _twilio = twilio;
            _logger = logger;
        }

        [HttpPost("{conversationId:int}/reply")]
        public async Task<IActionResult> Reply(int conversationId, [FromBody] SendReplyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Text é obrigatório.");

            var info = await _repo.GetConversationInfoAsync(conversationId);
            if (info == null)
                return NotFound();

            var (clientNumber, mode) = info.Value;

            // 1) Salva mensagem como Human (2)
            await _repo.AddMessageAsync(
                conversationId,
                from: 2,
                text: request.Text);

            // 2) Envia pelo Twilio pro cliente
            await _twilio.SendWhatsAppMessageAsync(clientNumber, request.Text);

            _logger.LogInformation("Resposta humana enviada para {Client}: {Text}", clientNumber, request.Text);

            return Ok();
        }
    }
}
