using Discord;
using Discord.Webhook;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RedditDiscordRSSBot {
    class Debug {
        const string PASTEEE_API_PUBLISH = "https://api.paste.ee/v1/pastes";
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

        public static void crash(Exception exception) {

            try {
                File.WriteAllText(
                    $"latest-crash-report.txt",
                    $"{DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString()}\n{exception.Message}\n{exception.StackTrace}"
                );

                if (Program.config != null && !String.IsNullOrWhiteSpace(Program.config.PasteeeApiKey)) {
                    string url = publishCrashReport(
                        $"Crash Report - {DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString()}",
                        exception.Message,
                        exception.StackTrace.ToString()
                    );

                    var embed = new EmbedBuilder {
                        Title = "Bot crashed.",
                        Description = $"View the exception and stack trace: [{url}]({url})",
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
            } catch (Exception) { }
            
            Environment.Exit(-1);
        }

        private static string publishCrashReport(string title, string message, string trace) {
            PasteeeResponse result = null;

            if (Program.config != null && !String.IsNullOrWhiteSpace(Program.config.PasteeeApiKey)) {
                PasteeeBody body = new PasteeeBody {
                    description = title,
                    sections = new PasteeeSection[] {
                        new PasteeeSection {
                            name = title,
                            contents = $"{title}\n{message}\nStack trace:\n{trace}"
                        }
                    }
                };

                string jsonBody = JsonConvert.SerializeObject(body);

                using (HttpClient client = new HttpClient()) {
                    client.DefaultRequestHeaders.Add("X-Auth-Token", Program.config.PasteeeApiKey);

                    //no need for real async in this method.
                    HttpResponseMessage response = client.PostAsync(PASTEEE_API_PUBLISH, new StringContent(jsonBody, Encoding.UTF8, "application/json")).Result;

                    if (response.StatusCode == System.Net.HttpStatusCode.Created) {
                        result = JsonConvert.DeserializeObject<PasteeeResponse>(response.Content.ReadAsStringAsync().Result);
                    }
                }
            }

            return (result != null && !String.IsNullOrWhiteSpace(result.link)) ? result.link : "";
        }

        public class PasteeeBody {
            public string description;
            public PasteeeSection[] sections;
        }

        public class PasteeeSection {
            public string name;
            public string contents;
        }

        public class PasteeeResponse {
            public string link;
        }
    }
}

