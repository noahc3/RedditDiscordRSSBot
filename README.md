# Reddit Discord RSS Bot

Bot that reads Reddit RSS feeds and sends new posts to Discord webhook(s).

Features:
- Any number of RSS feeds under one instance of the bot.
- Any number of webhooks for each RSS feed (different feeds can send to different webhooks).
- Filtering post titles with Regular Expressions.
- Displaying post titles in the Discord embed, or hiding them if preferred.
- Optionally embedding images if present.
- Choose between linking to the URL of the post, or the URL the post links to (when applicable).
- Adjustable scan interval.

When the bot scans an RSS feed, it will make note of the newest post that passes the Regex filter, and will only check for newer posts on the next scan.

Note that Reddit RSS feeds only show 25 posts at a time, and there is no page functionality in this bot, so this bot is not suited for extremely high traffic subreddits like /r/all or /r/popular.

## Creating Reddit RSS Feed URLs

See [https://www.reddit.com/wiki/rss](https://www.reddit.com/wiki/rss) for information on making Reddit RSS URLs. Any Reddit RSS URL is supported.

When you are trying to sort posts, it is important to include `?sort=[sort]` at the end of the URL (ex. `https://www.reddit.com/r/pics/new.rss?sort=new`) as otherwise the sort will occasionally not apply correctly.

## Creating Regular Expression Filters

To match any post, simply set the filter to `.*`.

Note that the RegEx engine is .NET. See [https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions) for information on .NET RegEx.

You can test your RegEx with [http://regexstorm.net/tester](http://regexstorm.net/tester), with the input text set to the value of the 'Title' element of an RSS entry.

## Configuration File

The configuration file is stored next to the executable in `config.json`. A default config file with no feeds will be created on first run.

### Primary Configuration Options

```
{
    "IntervalSeconds": [0+],
    "OutputToConsole": [true/false],
    "Feeds": [...]
}
```

- **IntervalSeconds**: The seconds paused between each scan of the RSS feed. Note this is time between the *starts* of two scans, not the end of one scan and start of the next.
- **OutputToConsole**: Output parsing information to standard output, including when scans occur, which scans are currently in process, and information of newly detected posts. Useful for debugging.
- **Feeds**: Array of Feed objects (see next section).

### Feed Configuration

```
{
    "FeedUrl": [url],
    "RegexWhitelist": [regex string],
    "DisplayTitles": [true/false],
    "EmbedImages": [true/false],
    "UseDirectLink": [true/false],
    "IncludeCommentsLink": [true/false],
    "DiscordWebhooks": [...],
    "LastReadTime": [DateTime parseable date+time string]
}
```

- **FeedUrl**: The URL of the Reddit RSS feed to parse
- **RegexWhitelist**: The RegEx pattern to test for matches. Posts will only be sent to the webhook if the title matches this string. Use `.*` to match anything.
- **DisplayTitles**: When true, the original post title is displayed in the embed. When false, `[Link]` is displayed instead of the post title.
- **EmbedImages**: When true, if the post is detected as linking to an image, the bot will parse the image URL and embed it. No effect on posts that do not directly link to an image.
- **UseDirectLink**: When false, the primary link in the embed will be for the Reddit post comments page. When true, the target link of the post will be parsed and used as the primary embed link. No effect on text posts.
- **IncludeCommentsLink**: When true, an additional direct link to the post comments will be included. Useful if you want to also link to comments when direct links are enabled for the primary embed URL.
- **DiscordWebhooks**: An object array of Discord webhooks (see below).
- **LastReadTime**: A datetime string parseable by .NET DateTime.Parse(). This is automatically updated by the bot when new posts are read, and only posts newer than this timestamp will be detected as new on the subsequent scan. Only edit this manually for debugging purposes.

### Webhook Configuration

{
    Endpoint: [url],
    RolePingIds: [...],
    UserPingIds: [...]
}

- **Endpoint**: The URL of the webhook endpoint.
- **RolePingIds**: A string array of role ID's to ping with each message sent to this specific webhook.
- **UserPingIds**: A string array of user ID's to ping with each message sent to this specific webhook.

### Example Configuration

```
{
  "IntervalSeconds": 60,
  "OutputToConsole": true,
  "Feeds": [
    {
      "FeedUrl": "https://www.reddit.com/r/pics/new.rss?sort=new",
      "RegexWhitelist": ".*",
      "DisplayTitles": false,
      "EmbedImages": true,
      "UseDirectLink": false,
      "IncludeCommentsLink": false,
      "DiscordWebhooks": [
        {
          "Endpoint": "https://discord.com/api/webhooks/123/abcd"
        }
      ],
      "LastReadTime": "2020-12-04 2:30:41 AM"
    },
    {
      "FeedUrl": "https://www.reddit.com/r/bapcsalescanada/new.rss?sort=new",
      "RegexWhitelist": "(?i)(gpu|graphic|video|3080)",
      "DisplayTitles": true,
      "EmbedImages": false,
      "UseDirectLink": true,
      "IncludeCommentsLink": true,
      "DiscordWebhooks": [
        {
          "Endpoint": "https://discord.com/api/webhooks/123/abcd",
          "PingUserIds": [
            "1234567890",
            "0987654321"
          ],
          "PingRoleIds": [
            "890678456123"
          ]
        },
        {
          "Endpoint": "https://discord.com/api/webhooks/456/efgh"
        }
      ],
      "LastReadTime": "2020-12-04 2:32:20 AM"
    }
  ]
}
```

The above configuration file will scan every 60 seconds for new posts and output newly detected posts to standard output.

The first feed will:
- scan /r/pics sorted by new,
- check for new posts with any title,
- not include the title of the post in the embed,
- embed the image if the post directly links to an image, 
- have the primary post link to the comments page,
- will not include a dedicated comments link beyond the primary URL, and
- send new post embeds to one webhook without any user or role pings.

The second feed will:
- scan /r/bapcsalescanada sorted by new,
- check for new posts with "gpu", "graphic", "video", or "3080" in the title, with `(?i)` marking the RegEx pattern as case insensitive,
- include the full post title in the embed,
- not embed any linked images,
- link to the target link provided by the post, or the comments page if not a link post,
- have a dedicated link to the comments page, and
- send new post embeds to two webhooks:
  - the messages to the first webhook will ping two users and one role
  - the messages to the second webhook will not ping any users or roles
