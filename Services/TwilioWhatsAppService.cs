using Twilio.Types;
using Twilio;
using WebAppTwilioApi.Settings;
using Microsoft.Extensions.Options;
using Twilio.Rest.Api.V2010.Account;

namespace WebAppTwilioApi.Services
{
    public class TwilioWhatsAppService
    {
        private readonly TwilioSettings _settings;

        public TwilioWhatsAppService(IOptions<TwilioSettings> options)
        {
            _settings = options.Value;

            TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
        }

        public Task<MessageResource> SendWhatsAppMessageAsync(string to, string body)
        {
            var messageOptions = new CreateMessageOptions(new PhoneNumber(to))
            {
                From = new PhoneNumber(_settings.WhatsAppFrom),
                Body = body
            };

            return MessageResource.CreateAsync(messageOptions);
        }
    }
}
