using CodeHollow.FeedReader;
using Discord;
using Discord.Webhook;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RedditDiscordRSSBot {
    class Program {
        private static Config config;
        private static Timer timer;
        static async Task Main(string[] args) {
            Console.WriteLine("Reddit RSS Bot by noahc3\n");
            LoadConfig();

            timer = new Timer((e) => { ParseFeeds(); }, null, TimeSpan.Zero, TimeSpan.FromSeconds(config.IntervalSeconds));

            await Task.Delay(Timeout.Infinite, new CancellationTokenSource().Token).ConfigureAwait(false);
        }

        private static void LoadConfig() {
            if (!File.Exists("config.json")) {
                config = new Config();
                File.WriteAllText("config.json", config.Serialize());
            }

            config = Config.Deserialize(File.ReadAllText("config.json"));

            foreach(RssFeed feed in config.Feeds) {
                feed.LastReadTimeDT = DateTime.Parse(feed.LastReadTime);
            }
        }

        private static void SaveConfig() {
            File.WriteAllText("config.json", config.Serialize());
        }

        private static void ParseFeeds() {

            if (config.OutputToConsole) Console.WriteLine($"\nParsing feeds at {DateTime.Now}");

            foreach(RssFeed k in config.Feeds) {
                k.LatestFeed = FeedReader.ReadAsync(k.FeedUrl).Result;
            }

            foreach(RssFeed k in config.Feeds) {
                ParseFeed(k);
            }

            SaveConfig();

            if (config.OutputToConsole) Console.WriteLine($"\nFinished parsing feeds at {DateTime.Now}");
        }

        private static void ParseFeed(RssFeed feed) {
            List<Post> newPosts = new List<Post>();
            DateTime newestReadTime = feed.LastReadTimeDT;

            if (config.OutputToConsole) Console.WriteLine($"\nParsing {feed.LatestFeed.Title} ({feed.FeedUrl})");
            
            foreach (FeedItem k in feed.LatestFeed.Items) {
                if (Regex.IsMatch(k.Title, feed.RegexWhitelist)) {
                    DateTime dt = DateTime.Parse(k.GetElement("updated"));
                    if (dt > feed.LastReadTimeDT) {
                        if (dt > newestReadTime) newestReadTime = dt;

                        bool hasImage = feed.EmbedImages && k.HasElement("thumbnail");
                        string directLink = (hasImage || feed.UseDirectLink) ? DirectLink(k) : "";

                        Post post = new Post() {
                            Title = feed.DisplayTitles ? k.Title : "[Link]",
                            Subreddit = k.GetElementAttribute("category", "term"),
                            Author = k.Author,
                            PublishDate = dt,
                            Url = feed.UseDirectLink ? directLink : k.Link,
                            HasImage = hasImage,
                            ImageUrl = hasImage ? directLink : ""
                        };
                        post.Embed = BuildDiscordEmbed(post);

                        newPosts.Add(post);
                    }
                }
            }

            if (newPosts.Count > 0) {
                feed.LastReadTimeDT = newestReadTime;
                feed.LastReadTime = newestReadTime.ToString();
                newPosts.Reverse();
                NotifyNewPosts(feed, newPosts);
            }
        }

        private static Embed BuildDiscordEmbed(Post post) {
            var embed = new EmbedBuilder {
                Title = post.Title,
                Description = $"Posted in /r/{post.Subreddit} by {post.Author}",
                Timestamp = post.PublishDate,
                Url = post.Url
            };

            if (post.HasImage) embed.ImageUrl = post.ImageUrl;

            return embed.Build();
        }

        private static void NotifyNewPosts(RssFeed feed, List<Post> posts) {
            for (int i = 0; i < posts.Count; i+=10) { //max 10 embeds per webhook push
                List<Embed> embeds = new List<Embed>();
                for (int k = i; k < Math.Min(i + 10, posts.Count); k++) {
                    Post p = posts.ElementAt(k);
                    embeds.Add(p.Embed);

                    if (config.OutputToConsole) {
                        Console.WriteLine($"\n{p.Title}");
                        Console.WriteLine($"Posted in /r/{p.Subreddit} by /u/{p.Author}");
                        Console.WriteLine($"Timestamp: {p.PublishDate}");
                        Console.WriteLine($"URL: {p.Url}");
                        if (p.HasImage) Console.WriteLine($"Image URL: {p.ImageUrl}");
                    }
                }

                foreach (string webhook in feed.DiscordWebhooks) {
                    using (var client = new DiscordWebhookClient(webhook)) {
                        client.SendMessageAsync(embeds: embeds).Wait();
                    }
                }
            }
        }

        private static string DirectLink(FeedItem item) {
            string magic = "(?<=<span><a\\ href\\=\\\").+?(?=\\\"\\>\\[link\\])";
            Match match = Regex.Match(item.Content, magic);
            if (match.Success) return match.Value;
            else return item.Link;
        }
    }
}
