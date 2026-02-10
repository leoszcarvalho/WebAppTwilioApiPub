using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAppTwilioApi.Data;

namespace WebAppTwilioApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly SqlConversationRepository _repo;

        public ConversationsController(SqlConversationRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<ActionResult<List<ConversationSummary>>> GetAll()
        {
            var list = await _repo.GetRecentConversationsAsync(50);
            return Ok(list);
        }

        [HttpGet("{id:int}/messages")]
        public async Task<ActionResult<List<MessageItem>>> GetMessages(int id)
        {
            var list = await _repo.GetMessagesAsync(id);
            return Ok(list);
        }
    }
}
