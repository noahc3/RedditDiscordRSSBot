using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedditDiscordRSSBot {
    public class DiscordWebhook {
        public string Endpoint = "";
        public string[] PingRoleIds = new string[0];
        public string[] PingUserIds = new string[0];
        public bool SendDebuggingInfo = false;

        [JsonIgnore]
        public string PingString = "";
    }
}
