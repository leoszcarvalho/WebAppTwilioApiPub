namespace WebAppTwilioApi.Models
{
    public class TwilioWebhookRequest
    {
        public string From { get; set; } = string.Empty; 
        public string Body { get; set; } = string.Empty; 
    }
}
