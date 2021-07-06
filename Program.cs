using CodeHollow.FeedReader;
using Discord;
using Discord.Webhook;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RedditDiscordRSSBot {
    class Program {
        public static Config config;
        private static Timer timer;
        private static Dictionary<string, bool> readPosts = new Dictionary<string, bool>();
        private static List<Tuple<double, List<string>>> timeClasses = new List<Tuple<double, List<string>>>();
        private static Comparer<Tuple<double, List<string>>> timeClassComparer = Comparer<Tuple<double, List<string>>>.Create((x, y) => x.Item1.CompareTo(y.Item1));
        private static DateTime LastReadPurge = DateTime.Parse("2000-1-01T00:00:00+00:00");

        static async Task Main(string[] args) {
            Console.WriteLine("Reddit RSS Bot by noahc3\n");

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CatchExit);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CatchExit);

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

            Debug.startup();

            foreach (RssFeed feed in config.Feeds) {
                feed.LastReadTimeDT = DateTime.Parse(feed.LastReadTime);
                if (feed.TrackType == 1) config.AnyReadPostStorage = true;
            }

            foreach (DiscordWebhook hook in config.Webhooks.Values) {
                hook.PingString = "";
                foreach (string id in hook.PingRoleIds) {
                    hook.PingString += $"<@&{id}> ";
                }

                foreach (string id in hook.PingUserIds) {
                    hook.PingString += $"<@{id}> ";
                }
            }

            if (config.AnyReadPostStorage && File.Exists("read.blob")) {
                string readBlob = File.ReadAllText("read.blob");
                string[] split;
                double ticks;
                int index;
                foreach(string k in readBlob.Split('\n')) {
                    if (k.Contains(':')) {
                        split = k.Split(':');
                        ticks = double.Parse(split[0]);
                        split = split[1].Split(',');
                        index = ~timeClasses.BinarySearch(new Tuple<double, List<string>>(ticks, null), timeClassComparer);
                        timeClasses.Insert(index, new Tuple<double, List<string>>(ticks, new List<string>()));
                        foreach (string j in split) {
                            timeClasses[index].Item2.Add(j);
                            readPosts[j] = true;
                        }
                    }
                }
            }

            Debug.loaded();
        }

        private static void SaveConfig() {
            File.WriteAllText("config.json", config.Serialize());

            if (config.AnyReadPostStorage) {
                string readBlob = "";
                string line;
                foreach (Tuple<double, List<string>> k in timeClasses) {
                    line = "";
                    line += $"{k.Item1}:";
                    foreach (string j in k.Item2) {
                        line += $"{j},";
                    }
                    if (line.Last() == ',') line = line.Substring(0, line.Length - 1);
                    line += '\n';
                    readBlob += line;
                }
                File.WriteAllText("read.blob", readBlob);
            }
        }

        private static void ParseFeeds() {

            if (config.OutputToConsole) Console.WriteLine($"\nParsing feeds at {DateTime.Now}");

            foreach (RssFeed k in config.Feeds) {
                k.LatestFeed = FeedReader.ReadAsync(k.FeedUrl).Result;
            }

            foreach (RssFeed k in config.Feeds) {
                ParseFeed(k);
            }

            if (config.AnyReadPostStorage) PurgeReadPosts();

            SaveConfig();

            if (config.OutputToConsole) Console.WriteLine($"\nFinished parsing feeds at {DateTime.Now}");
        }

        private static void ParseFeed(RssFeed feed) {
            List<Post> newPosts = new List<Post>();
            DateTime newestReadTime = feed.LastReadTimeDT;

            if (config.OutputToConsole) Console.WriteLine($"\nParsing {feed.LatestFeed.Title} ({feed.FeedUrl})");

            if (feed.TrackType == 0) { //track by post time
                foreach (FeedItem k in feed.LatestFeed.Items) {
                    if (Regex.IsMatch(k.Title, feed.RegexWhitelist)) {
                        DateTime dt = DateTime.Parse(k.GetElement("updated"));
                        if (dt > feed.LastReadTimeDT) {
                            if (dt > newestReadTime) newestReadTime = dt;
                            newPosts.Add(CreatePost(feed, k));
                        }
                    }
                }
            } else if (feed.TrackType == 1) { //track by storing read post id's from the last n hours
                foreach (FeedItem k in feed.LatestFeed.Items) {
                    if (!readPosts.ContainsKey(k.Id)) {
                        DateTime date = DateTime.Parse(k.GetElement("updated"));
                        if (date.AddHours(config.ReadPostRetentionTimeHours) > DateTime.Now) { //ignore posts older than the retention time, like announcements.
                            MarkPostRead(k.Id, DateTime.Parse(k.GetElement("updated")));
                            if (Regex.IsMatch(k.Title, feed.RegexWhitelist)) {
                                newPosts.Add(CreatePost(feed, k));
                            }
                        }
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

        private static void MarkPostRead(string id, DateTime time) {
            double tickClass = Math.Floor(time.Ticks / 36000000000.0);

            int index = timeClasses.BinarySearch(new Tuple<double, List<string>>(tickClass,null), timeClassComparer);
            if (index < 0) {
                index = ~index;
                timeClasses.Insert(index, new Tuple<double, List<string>>(tickClass, new List<string>()));
            }

            timeClasses[index].Item2.Add(id);
            readPosts[id] = true;
        }

        private static Post CreatePost(RssFeed feed, FeedItem k) {
            bool hasImage = feed.EmbedImages && k.HasElement("thumbnail");
            string directLink = (hasImage || feed.UseDirectLink) ? DirectLink(k) : "";

            Post post = new Post() {
                Title = feed.DisplayTitles ? k.Title : "[Link]",
                Subreddit = k.GetElementAttribute("category", "term"),
                Author = k.Author,
                PublishDate = DateTime.Parse(k.GetElement("updated")),
                Url = feed.UseDirectLink ? directLink : k.Link,
                CommentsUrl = feed.IncludeCommentsLink ? k.Link : null,
                HasImage = hasImage,
                ImageUrl = hasImage ? directLink : ""
            };

            if (post.Url.StartsWith("/")) post.Url = "https://www.reddit.com" + post.Url;
            post.Embed = BuildDiscordEmbed(post);

            return post;
        }

        private static Embed BuildDiscordEmbed(Post post) {
            var embed = new EmbedBuilder {
                Title = post.Title,
                Description = $"Posted in /r/{post.Subreddit} by {post.Author}" + (post.CommentsUrl != null ? $"\n[see comments]({post.CommentsUrl})" : ""),
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

                foreach (String target in feed.WebhookTargets) {
                    DiscordWebhook webhook = config.Webhooks[target];
                    using (var client = new DiscordWebhookClient(webhook.Endpoint)) {
                        client.SendMessageAsync(text: webhook.PingString.Length > 0 ? webhook.PingString : null, embeds: embeds).Wait();
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


        private static void PurgeReadPosts() {
            if (DateTime.Now.Subtract(LastReadPurge).TotalHours >= 1) {
                double purgeTimeClass = Math.Floor(DateTime.Now.AddHours(-1 * config.ReadPostRetentionTimeHours).Ticks / 36000000000.0);
                Tuple<double, List<string>> timeClass = timeClasses[0];
                while (timeClass != null && timeClass.Item1 <= purgeTimeClass) {
                    foreach (string k in timeClass.Item2) {
                        if (readPosts.ContainsKey(k)) readPosts.Remove(k);
                    }
                    timeClasses.Remove(timeClass);
                    if (timeClasses.Count() > 0) timeClass = timeClasses[0];
                    else timeClass = null;
                }
                LastReadPurge = DateTime.Now;
            }
        }

        static void CatchExit(object sender, EventArgs e) {
            Debug.exit();
        }
    }
}
