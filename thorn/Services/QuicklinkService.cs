using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace thorn.Services
{
    public class QuicklinkService
    {
        private readonly ScpService _scp;
        private readonly string _pattern;
        private readonly ulong[] _exemptChannelIds;
        private Dictionary<ulong, DateTime> _lastLink;
        private List<ulong> _latestLinkIds;
        private Dictionary<ulong, List<string>> _lastScp;

        private readonly TimeSpan _limit;

        public QuicklinkService(ScpService scp)
        {
            _scp = scp;
            _pattern = @"(?<=[^\/]|^)SCP(-| )[0-9]{3,4}((-| )(J|EX|ARC|SK|C[ZS]((-| )(J|EX|ARC)|))|)(?=\W|$)";
            _exemptChannelIds = new ulong[] {776117655119331369, 537063810121334784, 710575651664429127};
            _lastLink = new Dictionary<ulong, DateTime>();
            _latestLinkIds = new List<ulong>();
            _lastScp = new Dictionary<ulong, List<string>>();
            _limit = TimeSpan.FromSeconds(3);
        }
        public async Task<bool> CheckForScpReference(SocketUserMessage m)
        {
            var msg = (IMessage)m;

            if (_exemptChannelIds.Contains(m.Channel.Id)) return false;

            if (_lastLink.ContainsKey(m.Channel.Id))
            {
                if (DateTime.Now.Subtract(_lastLink[m.Channel.Id]) > _limit)
                    _lastLink[m.Channel.Id] = DateTime.Now;
                else
                    return false;
            }
            else
                _lastLink.Add(m.Channel.Id, DateTime.Now);
            
            var matches = Regex.Matches(m.Content, _pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (matches.Count == 0) return false;

            var mentions = new List<string>();
            foreach (Match match in matches)
            {
                if (!mentions.Contains(match.Value))
                    mentions.Add(match.Value);
            }

            if (msg.Type == MessageType.Reply && _lastScp.ContainsKey(msg.Reference.MessageId.GetValueOrDefault()))
            {
                ulong originalId = msg.Reference.MessageId.GetValueOrDefault();
                if (mentions.All(_lastScp[originalId].Contains))
                    return false;
            }

            mentions.Sort();
            LastScpAdd(m.Id, mentions);

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

        private void LastScpAdd(ulong id, List<string> _mentions)
        {
            if (_lastScp.Count > 3)
                _lastScp.Remove(_lastScp.Keys.First());
            _lastScp.Add(id, _mentions);
        }

        public bool IsRecentQuicklink(ulong messageId) => _latestLinkIds.Contains(messageId);
    }
}