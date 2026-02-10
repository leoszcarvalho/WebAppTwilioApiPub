using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace WebAppTwilioApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DbTestController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public DbTestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            var connString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connString))
                return StatusCode(500, "Connection string 'DefaultConnection' não encontrada.");

            try
            {
                using var conn = new SqlConnection(connString);
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM Conversations;";

                var result = await cmd.ExecuteScalarAsync();
                var count = Convert.ToInt32(result);

                return Ok(new
                {
                    message = "Conexão com o banco OK",
                    conversationsCount = count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Erro ao conectar no banco",
                    error = ex.Message
                });
            }
        }
    }
}
