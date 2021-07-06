using Discord;
using Discord.Webhook;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedditDiscordRSSBot {
    class Debug {
        public static void startup() {
            var embed = new EmbedBuilder {
                Title = "Bot started, parsing config...",
                Timestamp = DateTime.Now
            };

            Embed compiled = embed.Build();

            foreach (DiscordWebhook webhook in Program.config.Webhooks.Values) {
                if (webhook.SendDebuggingInfo) {
                    using (var client = new DiscordWebhookClient(webhook.Endpoint)) {
                        client.SendMessageAsync(embeds: new Embed[] { compiled }).Wait();
                    }
                }
            }
        }

        public static void loaded() {
            string desc = "Loaded the following feeds:";
            foreach (RssFeed feed in Program.config.Feeds) {
                desc += "\n" + feed.FeedUrl;
            }


            var embed = new EmbedBuilder {
                Title = "Bot loaded.",
                Description = desc,
                Timestamp = DateTime.Now
            };

            Embed compiled = embed.Build();

            foreach (DiscordWebhook webhook in Program.config.Webhooks.Values) {
                if (webhook.SendDebuggingInfo) {
                    using (var client = new DiscordWebhookClient(webhook.Endpoint)) {
                        client.SendMessageAsync(embeds: new Embed[] { compiled }).Wait();
                    }
                }
            }
        }

        public static void exit() {
            var embed = new EmbedBuilder {
                Title = "Bot shutdown.",
                Timestamp = DateTime.Now
            };

            Embed compiled = embed.Build();

            foreach (DiscordWebhook webhook in Program.config.Webhooks.Values) {
                if (webhook.SendDebuggingInfo) {
                    using (var client = new DiscordWebhookClient(webhook.Endpoint)) {
                        client.SendMessageAsync(embeds: new Embed[] { compiled }).Wait();
                    }
                }
            }
        }
    }
}
