using Microsoft.Data.SqlClient;

namespace WebAppTwilioApi.Data
{
    public class SqlConversationRepository
    {
        private readonly string _connectionString;

        public SqlConversationRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");
        }

        private SqlConnection CreateConnection()
            => new SqlConnection(_connectionString);

        public async Task<int> GetOrCreateConversationIdAsync(string clientNumber)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();

            // Procura conversa existente
            var cmdSelect = conn.CreateCommand();
            cmdSelect.CommandText = @"
                SELECT TOP 1 Id 
                FROM Conversations 
                WHERE ClientNumber = @clientNumber
                ORDER BY LastMessageAtUtc DESC;";
            cmdSelect.Parameters.AddWithValue("@clientNumber", clientNumber);

            var result = await cmdSelect.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }

            // Cria nova conversa
            var cmdInsert = conn.CreateCommand();
            cmdInsert.CommandText = @"
                INSERT INTO Conversations (ClientNumber, CreatedAtUtc, LastMessageAtUtc, Mode)
                OUTPUT INSERTED.Id
                VALUES (@clientNumber, SYSUTCDATETIME(), SYSUTCDATETIME(), 0); -- 0 = Bot
            ";
            cmdInsert.Parameters.AddWithValue("@clientNumber", clientNumber);

            var newId = await cmdInsert.ExecuteScalarAsync();
            return Convert.ToInt32(newId);
        }

        public async Task AddMessageAsync(int conversationId, byte from, string text, string? mediaUrl = null)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var tx = await conn.BeginTransactionAsync();

            try
            {
                var cmdInsertMsg = conn.CreateCommand();
                cmdInsertMsg.Transaction = (SqlTransaction)tx;
                cmdInsertMsg.CommandText = @"
                    INSERT INTO Messages (ConversationId, [From], [Text], TimestampUtc, MediaUrl)
                    VALUES (@conversationId, @from, @text, SYSUTCDATETIME(), @mediaUrl);
                ";
                cmdInsertMsg.Parameters.AddWithValue("@conversationId", conversationId);
                cmdInsertMsg.Parameters.AddWithValue("@from", from);
                cmdInsertMsg.Parameters.AddWithValue("@text", text);
                cmdInsertMsg.Parameters.AddWithValue("@mediaUrl", (object?)mediaUrl ?? DBNull.Value);

                await cmdInsertMsg.ExecuteNonQueryAsync();

                var cmdUpdateConvo = conn.CreateCommand();
                cmdUpdateConvo.Transaction = (SqlTransaction)tx;
                cmdUpdateConvo.CommandText = @"
                    UPDATE Conversations 
                    SET LastMessageAtUtc = SYSUTCDATETIME()
                    WHERE Id = @conversationId;
                ";
                cmdUpdateConvo.Parameters.AddWithValue("@conversationId", conversationId);

                await cmdUpdateConvo.ExecuteNonQueryAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ConversationSummary>> GetRecentConversationsAsync(int max = 50)
        {
            var list = new List<ConversationSummary>();

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT TOP (@max) 
                    c.Id,
                    c.ClientNumber,
                    c.Mode,
                    c.LastMessageAtUtc,
                    (
                        SELECT TOP 1 [Text] 
                        FROM Messages m 
                        WHERE m.ConversationId = c.Id
                        ORDER BY m.TimestampUtc DESC
                    ) AS LastMessageText
                FROM Conversations c
                ORDER BY c.LastMessageAtUtc DESC;
            ";
            cmd.Parameters.AddWithValue("@max", max);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ConversationSummary
                {
                    Id = reader.GetInt32(0),
                    ClientNumber = reader.GetString(1),
                    Mode = reader.GetByte(2),
                    LastMessageAtUtc = reader.GetDateTime(3),
                    LastMessagePreview = reader.IsDBNull(4) ? "" : reader.GetString(4)
                });
            }

            return list;
        }

        public async Task<List<MessageItem>> GetMessagesAsync(int conversationId)
        {
            var list = new List<MessageItem>();

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, [From], [Text], TimestampUtc, MediaUrl
                FROM Messages
                WHERE ConversationId = @conversationId
                ORDER BY TimestampUtc ASC;
            ";
            cmd.Parameters.AddWithValue("@conversationId", conversationId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new MessageItem
                {
                    Id = reader.GetInt32(0),
                    From = reader.GetByte(1),
                    Text = reader.GetString(2),
                    TimestampUtc = reader.GetDateTime(3),
                    MediaUrl = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }

            return list;
        }

        public async Task<(string ClientNumber, byte Mode)?> GetConversationInfoAsync(int conversationId)
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT ClientNumber, Mode 
                FROM Conversations
                WHERE Id = @id;
            ";
            cmd.Parameters.AddWithValue("@id", conversationId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (reader.GetString(0), reader.GetByte(1));
            }

            return null;
        }
    }

    public class ConversationSummary
    {
        public int Id { get; set; }
        public string ClientNumber { get; set; } = string.Empty;
        public byte Mode { get; set; }          // 0=Bot,1=Human
        public DateTime LastMessageAtUtc { get; set; }
        public string LastMessagePreview { get; set; } = string.Empty;
    }

    public class MessageItem
    {
        public int Id { get; set; }
        public byte From { get; set; }          // 0=Client,1=Bot,2=Human
        public string Text { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
        public string? MediaUrl { get; set; }
    }

}
