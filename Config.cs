using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedditDiscordRSSBot {
    public class Config {
        public int IntervalSeconds = 60;
        public bool OutputToConsole = false;
        public RssFeed[] Feeds = new RssFeed[0];

        

        public string Serialize() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static Config Deserialize(string text) {
            return JsonConvert.DeserializeObject<Config>(text);
        }
    }
}
