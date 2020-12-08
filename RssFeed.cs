using CodeHollow.FeedReader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedditDiscordRSSBot {
    public class RssFeed {
        public string FeedUrl = "";
        public string RegexWhitelist = "";
        public bool DisplayTitles = true;
        public bool EmbedImages = true;
        public bool UseDirectLink = true;
        public bool IncludeCommentsLink = true;
        public DiscordWebhook[] DiscordWebhooks = new DiscordWebhook[0];
        public string LastReadTime = "2000-1-01T00:00:00+00:00";

        [JsonIgnore]
        public Feed LatestFeed;

        [JsonIgnore]
        public DateTime LastReadTimeDT;
    }
}
