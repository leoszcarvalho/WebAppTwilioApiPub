using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Twilio.Security;
using WebAppTwilioApi.Models;
using WebAppTwilioApi.Services;
using WebAppTwilioApi.Data;
using WebAppTwilioApi.Settings;

namespace WebAppTwilioApi.Controllers
{
    [Route("twilio/webhook")]
    [ApiController]
    [AllowAnonymous] // Twilio precisa chamar sem token Azure AD
    public class TwilioWebhookController : ControllerBase
    {
        private readonly OpenAiChatService _openAiService;
        private readonly TwilioWhatsAppService _twilioService;
        private readonly SqlConversationRepository _conversationRepo;
        private readonly ILogger<TwilioWebhookController> _logger;
        private readonly string _twilioAuthToken;
        private readonly IWebHostEnvironment _env;

        public TwilioWebhookController(
            OpenAiChatService openAiService,
            TwilioWhatsAppService twilioService,
            SqlConversationRepository conversationRepo,
            ILogger<TwilioWebhookController> logger,
            IOptions<TwilioSettings> twilioOptions,
            IWebHostEnvironment env)
        {
            _openAiService = openAiService;
            _twilioService = twilioService;
            _conversationRepo = conversationRepo;
            _logger = logger;
            _env = env;

            _twilioAuthToken = twilioOptions?.Value?.AuthToken ?? string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> Receive([FromForm] TwilioWebhookRequest request)
        {
            // Segurança básica: se não tem AuthToken configurado, não dá pra validar.
            if (string.IsNullOrWhiteSpace(_twilioAuthToken))
            {
                _logger.LogError("Twilio AuthToken não está configurado (Twilio:AuthToken).");
                return StatusCode(500, "Twilio AuthToken não configurado.");
            }

            // ✅ (Opcional) Permite testar localmente no Postman sem assinatura.
            // Em produção, continua exigindo assinatura.
            if (_env.IsDevelopment())
            {
                _logger.LogWarning("DEV MODE: validação de assinatura Twilio ignorada.");
                // Se você quiser ignorar MESMO (retornar 200 direto), descomente:
                // return Ok("DEV MODE: ok");
            }

            // Se NÃO estiver em dev, valida assinatura
            if (!_env.IsDevelopment())
            {
                // 1) Monta URL "vista externamente" (evita problema de proxy/https no Azure)
                var scheme = Request.Headers["X-Forwarded-Proto"].ToString();
                if (string.IsNullOrWhiteSpace(scheme))
                    scheme = Request.Scheme;

                var requestUrl = $"{scheme}://{Request.Host}{Request.Path}{Request.QueryString}";

                // 2) Lê o form e transforma em dict (Twilio usa form-urlencoded)
                var form = await Request.ReadFormAsync();
                var parameters = form.ToDictionary(x => x.Key, x => x.Value.ToString());

                // 3) Pega assinatura do header
                var twilioSignature = Request.Headers["X-Twilio-Signature"].ToString();

                if (string.IsNullOrWhiteSpace(twilioSignature))
                {
                    _logger.LogWarning("Webhook sem X-Twilio-Signature. IP: {Ip}", HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized();
                }

                // 4) Valida assinatura
                var validator = new RequestValidator(_twilioAuthToken);
                var isValid = validator.Validate(requestUrl, parameters, twilioSignature);

                if (!isValid)
                {
                    _logger.LogWarning("Assinatura Twilio inválida. URL: {Url} IP: {Ip}", requestUrl, HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized();
                }
            }

            // 5) A partir daqui: é Twilio legítimo (ou dev mode)
            try
            {
                var clientNumber = request?.From?.Trim();
                var body = request?.Body ?? string.Empty;

                if (string.IsNullOrWhiteSpace(clientNumber))
                {
                    _logger.LogWarning("Webhook recebido sem campo From.");
                    return BadRequest("From é obrigatório.");
                }

                _logger.LogInformation("Mensagem recebida de {From}: {Body}", clientNumber, body);

                var conversationId = await _conversationRepo.GetOrCreateConversationIdAsync(clientNumber);

                // 0 = Client
                await _conversationRepo.AddMessageAsync(conversationId, from: 0, text: body);

                var reply = await _openAiService.GenerateReplyAsync(body);

                // 1 = Bot
                await _conversationRepo.AddMessageAsync(conversationId, from: 1, text: reply);

                await _twilioService.SendWhatsAppMessageAsync(clientNumber, reply);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar webhook do Twilio");
                // Você pode escolher retornar 200 para evitar retry do Twilio ou 500 para retry
                return Ok();
            }
        }
    }
}
