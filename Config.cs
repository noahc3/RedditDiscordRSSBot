using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedditDiscordRSSBot {
    public class Config {
        public int IntervalSeconds = 60;
        public bool OutputToConsole = false;
        public long ReadPostRetentionTimeHours = 168;
        public Dictionary<string, DiscordWebhook> Webhooks = new Dictionary<string, DiscordWebhook>();
        public RssFeed[] Feeds = new RssFeed[0];
        

        [JsonIgnore]
        public bool AnyReadPostStorage = false;

        

        public string Serialize() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static Config Deserialize(string text) {
            return JsonConvert.DeserializeObject<Config>(text);
        }
    }
}
