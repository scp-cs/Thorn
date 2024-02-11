# Install

This Discord bot is made *very specifically* for the [Czech branch Discord chat](https://discord.gg/ZAdfEJ4); other then for testing purposes, there is probably no need for you to try to run this bot, or even read this guide. Why am I writing this.

## Setup

### Dependencies
You have to have [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) SDK installed, and that's about it.

### Clone
```shell
$ git clone https://github.com/scp-cs/Thorn.git
```

### Build
In the project root directory:
```shell
$ dotnet build -c Release
```
And you're all set! The output is in `thorn/bin/Release/net8.0/`

## Configuration
Now the fun part. This bot has multiple configuration files that need to be set up appropriately.

### `config.json`
This is self-explanatory

```json
{
  "token": "YOUR-BOT-TOKEN",
  "prefix": "-"
}
```

### `feeds.json`
This is a file that defines how and what RSS feeds are being read. Below is an example of such one.

```json
[
   {
    "Link": "http://scp-cs.wikidot.com/feed/site-changes.xml",
    "ChannelIds": [800776102236324294],
    "Filter": ["new page"],
    "EmbedColor": 16711680,
    "NewPageAnnouncement": true,
    "RequireAuth": false
  },
  {
    "Link": "http://scp-cs.wikidot.com/feed/site-changes.xml",
    "ChannelIds": [735012092329918297],
    "EmbedColor": 16711680,
    "NewPageAnnouncement": false,
    "RequireAuth": false
  }
]
```
You don't have to have any feeds set up (in that case just put `[]` in the file).

### `constants.json` and `daily.json`

These are static configuration files and they are not meant to be changed, but you can do so if you wish.

## Done

Hooray! You made it. Now enjoy your very own Thorn. 