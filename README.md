# Thorn
[![.NET Actions Status](https://github.com/scp-cs/Thorn/workflows/.NET/badge.svg)](https://github.com/scp-cs/Thorn/actions) [![Discord chat](https://img.shields.io/discord/536983829437480984?logo=discord)](https://discord.gg/ZAdfEJ4) ![Release](https://img.shields.io/github/release/scp-cs/Thorn.svg) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Thorn is a Discord Bot made specifically for the Czech branch Discord chat.

## Features
* Custom user accounts
	* Points for translating, writing, and correcting
	* Links to Wikidot profile, author/translator page, and sandbox
	* Custom description and color
	* Leaderboards
* Quick links to important guides/hubs
* Daily reminder with important events in the past and name-days

## Install
This bot is private, and there are no plans on making it public. However you are free to run Thorn locally yourself.

1. Clone this repo
2. Create a `config.json` file in `thorn/Config/` that looks like this:
```json
{
   "token": "<DISCORD-BOT-TOKEN>",
   "prefix": "."
}
```
3. Run `dotnet build --configuration Release` in the root directory
4. You're all set! Output is in `thorn/bin/Release/netcoreapp3.1/`

## License
All of the code is licensed under the [MIT license](https://opensource.org/licenses/MIT). All of the images (namely the [stop sign](https://github.com/scp-cs/Thorn/blob/master/thorn/Media/stop.png)) are licensed under the [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) license.