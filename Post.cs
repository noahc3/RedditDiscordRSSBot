using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedditDiscordRSSBot {
    public class Post {
        public string Title;
        public string Subreddit;
        public string Author;
        public DateTime? PublishDate;
        public string Url;
        public string CommentsUrl;
        public bool HasImage;
        public string ImageUrl;
        public bool LowQualityNotice = false;

        public Embed Embed;
    }
}
