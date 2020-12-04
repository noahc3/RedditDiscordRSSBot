using CodeHollow.FeedReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedditDiscordRSSBot {
    public static class Extensions {
        public static bool HasElement(this FeedItem item, string key) {
            return item.SpecificItem.Element.Descendants().Any(x => x.Name.LocalName == key);
        }
        public static string GetElement(this FeedItem item, string key) {
            return item.SpecificItem.Element.Descendants().FirstOrDefault(x => x.Name.LocalName == key).Value;
        }

        public static string GetElementAttribute(this FeedItem item, string key, string attribute) {
            return item.SpecificItem.Element.Descendants().FirstOrDefault(x => x.Name.LocalName == key).Attribute(attribute)?.Value;
        }
    }
}
