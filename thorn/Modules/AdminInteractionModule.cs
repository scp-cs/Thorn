using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace thorn.Modules;

public class AdminInteractionModule(ILogger<AdminInteractionModule> logger, IConfiguration config) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("stop", "Pošle stopku")]
    [RequireRole("O5", Group = "Admins")]
    [RequireRole("O4", Group = "Admins")]
    public async Task Stop()
    {
        await RespondWithFileAsync("Media/stop.png");
        logger.LogInformation("{ContextUser} used STOP in #{ContextChannel}", Context.User, Context.Channel);
    }

    [SlashCommand("řekni", "Něco řekne")]
    [RequireRole("O5", Group = "Admins")]
    [RequireRole("O4", Group = "Admins")]
    public async Task Say([ChannelTypes(ChannelType.Text)] ISocketMessageChannel channel, string text)
    {
        await channel.SendMessageAsync(text);
        await RespondAsync($"zpráva poslána do kanálu {channel}!", ephemeral: true);
    }
    
    [SlashCommand("hlasování", "Zahájí hlasování")]
    [RequireRole("O5", Group = "Admins")]
    [RequireRole("O4", Group = "Admins")]
    public async Task Vote([ChannelTypes(ChannelType.Text)] ISocketMessageChannel channel, string text)
    {
        var msg = await channel.SendMessageAsync(text);
        await AddVoteEmotes(msg);
        await RespondAsync($"hlasování zahájeno v kanálu {channel}!", ephemeral: true);
    }
    
    private async Task AddVoteEmotes(RestUserMessage msg)
    {
        await msg.AddReactionAsync(Emote.Parse(config["emotes:yes"]));
        await msg.AddReactionAsync(Emote.Parse(config["emotes:no"]));
        await msg.AddReactionAsync(Emote.Parse(config["emotes:abstain"]));

        // I swear to god, since when are there 2 different eye emojis?
        // await msg.AddReactionAsync(new Emoji("👁️")); This one doesn't work, I think
        await msg.AddReactionAsync(new Emoji("👁"));
    }
}