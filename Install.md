# Install

This Discord bot is made *very specifically* for the [Czech branch Discord chat](https://discord.gg/ZAdfEJ4); other than for testing purposes,
there is probably no need for you to run this bot, or even read this guide. Why am I writing this.

## Setup

### Dependencies
You have to have [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) SDK installed.

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
`config.json` is where most of the magic happens. Be sure to fill out the token field.

`feeds.json` is for configuring the RSS fetching. If you don't wish to fetch any RSS
feeds, just put `[]` (an ampty array) in the file.

There are examples in `config.example.json` and `feeds.example.json` respectively.

`daily.json` is not really meant to be edited, but you surely get the idea.

## Deploy
I use a simple systemd service.

```ini
[Unit]
Description=Thorn - the SCP-CS bot

[Service]
Type=simple
Restart=always
WorkingDirectory=/home/thorn/Thorn/thorn/bin/Release/net8.0/
ExecStart=/usr/bin/dotnet /home/thorn/Thorn/thorn/bin/Release/net8.0/thorn.dll

[Install]
WantedBy=multi-user.target
```

Now enjoy your very own Thorn 🥳 If you need help, message me (you can find me on the SCP-CS discord server and [elsewhere](https://chamik.eu/contact/)).
