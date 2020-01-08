using BotCoreNET;
using Discord;
using Discord.WebSocket;
using JSON;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.Moderation
{
    struct ChannelModerationEntry : IJSONSerializable
    {
        public readonly ulong GuildId;
        public ChannelModerationType Type;
        public DateTimeOffset Timestamp;
        public ulong ChannelId;
        public string ChannelName;
        public ulong ActorId;
        public string ActorName;
        public string Info;

        public ChannelModerationEntry(ulong guildId) : this()
        {
            GuildId = guildId;
        }

        public ChannelModerationEntry(ulong guildId, ChannelModerationType type, SocketGuildChannel channel, SocketGuildUser actor, string info = null) 
        {
            GuildId = guildId;
            Type = type;
            Timestamp = DateTimeOffset.UtcNow;
            ChannelId = channel.Id;
            ChannelName = channel.Name;
            ActorId = actor.Id;
            ActorName = actor.ToString();
            Info = info;
        }

        public EmbedBuilder ToEmbed()
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = Type.ToString()
                },
                Timestamp = Timestamp,
            };
            string channelName = ChannelName;
            SocketGuild guild = BotCore.Client.GetGuild(GuildId);
            if (guild != null)
            {
                SocketTextChannel channel = guild.GetTextChannel(ChannelId);
                if (channel != null)
                {
                    channelName = channel.Name;
                }
            }
            switch (Type)
            {
                case ChannelModerationType.Locked:
                    embed.Title = "Locked " + channelName;
                    break;
                case ChannelModerationType.Unlocked:
                    embed.Title = "Unlocked " + channelName;
                    break;
                case ChannelModerationType.Purged:
                    embed.Title = "Purged Messages in " + channelName;
                    break;
            }
            string actorName = ActorName;
            if (guild != null)
            {
                SocketGuildUser actor = guild.GetUser(ActorId);
                if (actor != null)
                {
                    actorName = actor.Mention;
                }
            }
            if (Info == null)
            {
                embed.Description = "Actor: " + actorName;
            }
            else
            {
                embed.Description = $"Actor: {actorName}\n{Info}";
            }
            return embed;
        }

        #region JSON

        private const string JSON_TIMESTAMP = "Timestamp";
        private const string JSON_MODTYPE = "Type";
        private const string JSON_CHANNELID = "ChannelId";
        private const string JSON_ACTORID = "ActorId";
        private const string JSON_CHANNELNAME = "ChannelName";
        private const string JSON_ACTORNAME = "ActorName";
        private const string JSON_INFO = "Info";

        public bool FromJSON(JSONContainer json)
        {
            if (json.TryGetField(JSON_MODTYPE, out uint type) && json.TryGetField(JSON_CHANNELID, out ulong channelId) && json.TryGetField(JSON_ACTORID, out ulong actorid))
            {
                Type = (ChannelModerationType)type;
                ChannelId = channelId;
                ActorId = actorid;
                if (json.TryGetField(JSON_TIMESTAMP, out string timestamp_str))
                {
                    if (DateTimeOffset.TryParseExact(timestamp_str, "u", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset timestamp))
                    {
                        Timestamp = timestamp;
                    }
                    else
                    {
                        Timestamp = DateTimeOffset.MinValue;
                    }
                }
                json.TryGetField(JSON_CHANNELNAME, out string channelname);
                ChannelName = channelname;
                json.TryGetField(JSON_ACTORNAME, out string actorname);
                ActorName = actorname;
                json.TryGetField(JSON_INFO, out string info);
                Info = info;
                return true;
            }
            return false;
        }

        public JSONContainer ToJSON()
        {
            JSONContainer json = JSONContainer.NewObject();
            json.TryAddField(JSON_MODTYPE, (uint)Type);
            json.TryAddField(JSON_CHANNELID, ChannelId);
            json.TryAddField(JSON_ACTORID, ActorId);
            json.TryAddField(JSON_TIMESTAMP, Timestamp.ToString("u"));
            json.TryAddField(JSON_CHANNELNAME, ChannelName);
            json.TryAddField(JSON_ACTORNAME, ActorName);
            if (!string.IsNullOrEmpty(Info))
            {
                json.TryAddField(JSON_INFO, Info);
            }
            return json;
        }

        #endregion
    }
}
