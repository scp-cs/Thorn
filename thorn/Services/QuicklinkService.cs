using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace thorn.Services;

public class QuicklinkService
{
    private readonly ScpService _scp;
    private readonly string _pattern;
    private readonly ulong[] _exemptChannelIds;
    private Dictionary<ulong, DateTime> _lastLinkInChannel;
    private List<ulong> _latestLinkIds;

    private readonly TimeSpan _limit;

    public QuicklinkService(ScpService scp)
    {
        _scp = scp;
        _pattern = @"(?<=[^\/]|^)SCP(-| )[0-9]{3,4}((-| )(J|EX|ARC|SK|C[ZS]((-| )(J|EX|ARC)|))|)(?=\W|$)";
        _exemptChannelIds = new ulong[] { 776117655119331369, 537063810121334784, 710575651664429127 };
        _lastLinkInChannel = new Dictionary<ulong, DateTime>();
        _latestLinkIds = new List<ulong>();
        _limit = TimeSpan.FromSeconds(10);
    }

    public async Task<bool> CheckForScpReference(SocketUserMessage m)
    {
        if (_exemptChannelIds.Contains(m.Channel.Id)) return false;

        var matches = Regex.Matches(m.Content, _pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (matches.Count == 0) return false;

        if (_lastLinkInChannel.ContainsKey(m.Channel.Id))
        {
            if (DateTime.Now.Subtract(_lastLinkInChannel[m.Channel.Id]) > _limit)
                _lastLinkInChannel[m.Channel.Id] = DateTime.Now;
            else
                return false;
        }
        else
        {
            _lastLinkInChannel.Add(m.Channel.Id, DateTime.Now);
        }

        var mentions = new List<string>();
        foreach (Match match in matches)
            if (!mentions.Contains(match.Value))
                mentions.Add(match.Value);

        mentions.Sort();

        var quicklink = await m.Channel.SendMessageAsync(embed: await _scp.GetEmbedForReference(mentions));
        await quicklink.AddReactionAsync(new Emoji("🗞️"));
        TrackIdAdd(quicklink.Id);
        return true;
    }

    private void TrackIdAdd(ulong id)
    {
        if (_latestLinkIds.Count > 2)
            _latestLinkIds.RemoveAt(_latestLinkIds.Count - 1);
        _latestLinkIds.Add(id);
    }

    public bool IsRecentQuicklink(ulong messageId)
    {
        return _latestLinkIds.Contains(messageId);
    }
}