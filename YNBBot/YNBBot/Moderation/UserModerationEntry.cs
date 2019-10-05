using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BotCoreNET;
using Discord.WebSocket;
using JSON;

namespace YNBBot.Moderation
{
    struct UserModerationEntry : IJSONSerializable
    {
        public readonly ulong GuildId;
        public ModerationType Type { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }
        public string Description { get; private set; }
        public string Info { get; private set; }
        public ulong ActorId { get; private set; }
        public string ActorName { get; private set; }

        public UserModerationEntry(ulong guildId)
        {
            GuildId = guildId;
            Type = ModerationType.Undefined;
            Timestamp = default;
            Description = default;
            Info = default;
            ActorId = default;
            ActorName = default;
        }

        public UserModerationEntry(ulong guildId, ModerationType type, DateTimeOffset? timestamp, SocketGuildUser actor, string description = null, string info = null)
        {
            GuildId = guildId;
            Type = type;
            if (timestamp.HasValue)
            {
                Timestamp = timestamp.Value;
            }
            else
            {
                Timestamp = DateTimeOffset.UtcNow;
            }
            Description = description;
            Info = info;
            ActorId = actor.Id;
            ActorName = actor.ToString();
        }

        public override string ToString()
        {
            string actor_str = ActorName == null ? ActorId.ToString() : ActorName;
            string timestamp_str = Timestamp == DateTimeOffset.MinValue ? "No Timestamp" : Timestamp.ToString("u");
            string info_str = string.IsNullOrEmpty(Info) ? string.Empty : $" - {Info}";
            string descr_str = string.IsNullOrEmpty(Description) ? string.Empty : $" `{Description}`";

            SocketGuild guild = BotCore.Client.GetGuild(GuildId);
            SocketGuildUser actor = null;
            if (guild != null)
            {
                actor = guild.GetUser(ActorId);
                if (actor != null)
                {
                    actor_str = actor.Mention;
                }
            }
            return $"[**{Type}** - {timestamp_str} - {actor_str}{info_str}]{descr_str}";
        }

        const string JSON_TYPE = "Type";
        const string JSON_TIMESTAMP = "Timestamp";
        const string JSON_DESCR = "Description";
        const string JSON_INFO = "Info";
        const string JSON_ACTORID = "ActorId";
        const string JSON_ACTORNAME = "ActorName";

        public bool FromJSON(JSONContainer json)
        {
            if (json.TryGetField(JSON_TYPE, out int type_i) && json.TryGetField(JSON_ACTORID, out ulong actorId))
            {
                Type = (ModerationType)type_i;
                ActorId = actorId;

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

                if (json.TryGetField(JSON_DESCR, out string descr))
                {
                    Description = descr;
                }
                else
                {
                    Description = null;
                }

                if (json.TryGetField(JSON_INFO, out string info))
                {
                    Info = info;
                }
                else
                {
                    Info = null;
                }

                if (json.TryGetField(JSON_ACTORNAME, out string actorname))
                {
                    ActorName = actorname;
                }
                else
                {
                    ActorName = null;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public JSONContainer ToJSON()
        {
            JSONContainer result = JSONContainer.NewObject();
            result.TryAddField(JSON_TYPE, (int)Type);
            result.TryAddField(JSON_TIMESTAMP, Timestamp.ToString("u"));
            if (!string.IsNullOrEmpty(Description))
            {
                result.TryAddField(JSON_DESCR, Description);
            }
            if (!string.IsNullOrEmpty(Info))
            {
                result.TryAddField(JSON_INFO, Info);
            }
            result.TryAddField(JSON_ACTORID, ActorId);
            result.TryAddField(JSON_ACTORNAME, ActorName);
            return result;
        }
    }

    public enum ModerationType : byte
    {
        Note = 0,
        Warning = 1,
        Muted = 10,
        UnMuted = 11,
        Kicked = 20,
        Banned = 30,
        UnBanned = 31,
        Undefined = byte.MaxValue
    }
}
