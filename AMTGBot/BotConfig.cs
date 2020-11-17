using System.Collections.Generic;

namespace AMTGBot
{
    public sealed record BotConfig
    {
        public string Token { get; set; }
        public int ComputationTimeLimit { get; set; }
        public int PhotoStorageChatId { get; set; }
    }
}
