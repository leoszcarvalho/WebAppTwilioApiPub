using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using WebAppTwilioApi.Settings;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebAppTwilioApi.Services
{
    public class OpenAiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAiSettings _settings;

        public OpenAiChatService(HttpClient httpClient, IOptions<OpenAiSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }

        public async Task<string> GenerateReplyAsync(string userMessage)
        {
            var systemPrompt = """
                Você é a atendente virtual do salão "Studio Tamara Sá".
                Fale em PT-BR, tom simpático e objetivo.
                Serviços e preços:
                - Corte feminino: R$80
                - Hidratação: R$120 (60 min)
                Endereço: Rua das Flores, 123 - Centro.
                Horários sugeridos: amanhã 14:00, 15:30, 17:00.
                Regras:
                - Se pedirem preço, responda claro e convide para agendar.
                - Se pedirem disponibilidade, ofereça os três horários e peça a preferência.
                - Não confirme agendamento sozinho; colete {serviço, dia/turno, nome}. Diga que você pode confirmar.
                - Seja sempre educada, breve e útil.
            """;

            var payload = new
            {
                model = _settings.Model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userMessage ?? string.Empty }
                },
                temperature = 0.5
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var resp = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                // LOGAR json se quiser
                var vr1 = $"[OpenAI ERROR] Status: {(int)resp.StatusCode} {resp.StatusCode}";
                Console.WriteLine(json);
                return $"Erro OpenAI ({(int)resp.StatusCode}): {json}  {vr1}";
            }

            using var doc = JsonDocument.Parse(json);
            var reply = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return reply?.Trim() ?? "Posso te ajudar com preços e horários";
        }
    }
}
