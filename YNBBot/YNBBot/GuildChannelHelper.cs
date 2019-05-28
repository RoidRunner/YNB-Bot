using Discord.WebSocket;
using System;
using System.Collections.Generic;

namespace YNBBot
{
    static class GuildChannelHelper
    {
        public static ulong DebugChannelId { get; set; }
        public static ulong WelcomingChannelId { get; set; }
        public static GuildChannelInformation DebugChannelInfo
        {
            get
            {
                if (channelInfos.TryGetValue(DebugChannelId, out GuildChannelInformation debugChannel))
                { return debugChannel; }
                else
                {
                    return null;
                }
            }
        }
        public static GuildChannelInformation WelcomingChannelInfo
        {
            get
            {
                if (channelInfos.TryGetValue(WelcomingChannelId, out GuildChannelInformation welcomingChannel))
                { return welcomingChannel; }
                else
                {
                    return null;
                }
            }
        }

        private const string JSON_DEBUGCHANNELID = "DebugChannelId";
        private const string JSON_WELCOMINGCHANNELID = "WelcomingChannelId";
        private const string JSON_CHANNELINFOS = "ChannelInfos";
        private static Dictionary<ulong, GuildChannelInformation> channelInfos = new Dictionary<ulong, GuildChannelInformation>();

        public static GuildChannelInformation GetChannelInfoOrDefault(SocketGuildChannel channel)
        {
            if (channelInfos.TryGetValue(channel.Id, out GuildChannelInformation channelInformation))
            {
                return channelInformation;
            }
            else
            {
                return new GuildChannelInformation(channel.Id);
            }
        }

        public static bool TryGetChannelInfo(ulong channelId, out GuildChannelInformation channelInformation)
        {
            return channelInfos.TryGetValue(channelId, out channelInformation);
        }

        public static void SetChannelInfo(GuildChannelInformation channelInformation)
        {
            if (channelInfos.ContainsKey(channelInformation.Id))
            {
                if (channelInformation.IsDefault)
                {
                    channelInfos.Remove(channelInformation.Id);
                }
                else
                {
                    channelInfos[channelInformation.Id] = channelInformation;
                }
            }
            else
            {
                if (!channelInformation.IsDefault)
                {
                    channelInfos.Add(channelInformation.Id, channelInformation);
                }
            }
        }

        public static bool TryGetChannel(ulong channelId, out SocketGuildChannel channel)
        {
            channel = null;
            foreach (SocketGuild guild in Var.client.Guilds)
            {
                channel = guild.GetChannel(channelId);
                if (channel != null)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetChannel<T>(ulong channelId, out T channel) where T : SocketGuildChannel
        {
            if (TryGetChannel(channelId, out SocketGuildChannel gChannel))
            {
                channel = gChannel as T;
                return channel != null;
            }
            else
            {
                channel = null;
                return false;
            }
        }

        public static void FromJSON(JSONObject json)
        {
            channelInfos.Clear();
            DebugChannelId = 0;
            WelcomingChannelId = 0;

            if (json.GetField(out string debugChannelId_str, JSON_DEBUGCHANNELID, null))
            {
                if (ulong.TryParse(debugChannelId_str, out ulong debugChannelId))
                {
                    DebugChannelId = debugChannelId;
                }
            }
            if (json.GetField(out string welcomingChannelId_str, JSON_WELCOMINGCHANNELID, null))
            {
                if (ulong.TryParse(welcomingChannelId_str, out ulong welcomingChannelId))
                {
                    WelcomingChannelId = welcomingChannelId;
                }
            }

            JSONObject channelInfoArray = json[JSON_CHANNELINFOS];
            if ((channelInfoArray != null) && channelInfoArray.IsArray)
            {
                foreach (JSONObject channelInfo in channelInfoArray)
                {
                    GuildChannelInformation info = new GuildChannelInformation();
                    if (info.FromJSON(channelInfoArray))
                    {
                        channelInfos.Add(info.Id, info);
                    }
                }
            }
        }

        public static JSONObject ToJSON()
        {
            JSONObject json = new JSONObject();

            json.AddField(JSON_DEBUGCHANNELID, DebugChannelId.ToString());
            json.AddField(JSON_WELCOMINGCHANNELID, WelcomingChannelId.ToString());

            JSONObject channelInfoArray = new JSONObject();
            foreach (GuildChannelInformation channelInfo in channelInfos.Values)
            {
                channelInfoArray.Add(channelInfo.ToJSON());
            }
            json.AddField(JSON_CHANNELINFOS, channelInfoArray);

            return json;
        }
    }

    public class GuildChannelInformation : IJSONSerializable, ICloneable
    {
        public const bool DEFAULT_ALLOWCOMMANDS = true;
        public const bool DEFAULT_ALLOWSHITPOSTING = false;

        public ulong Id;
        public bool AllowCommands;
        public bool AllowShitposting;

        public bool IsDebugChannel { get { return Id == GuildChannelHelper.DebugChannelId; } }
        public bool IsWelcomingChannel { get { return Id == GuildChannelHelper.WelcomingChannelId; } }

        public bool IsDefault
        {
            get
            {
                return AllowCommands == DEFAULT_ALLOWCOMMANDS && AllowShitposting == DEFAULT_ALLOWSHITPOSTING;
            }
        }

        public GuildChannelInformation()
        {

        }

        public GuildChannelInformation(ulong id, bool allowCommands = DEFAULT_ALLOWCOMMANDS, bool allowShitposting = DEFAULT_ALLOWSHITPOSTING)
        {
            Id = id;
            AllowCommands = allowCommands;
            AllowShitposting = allowShitposting;
        }

        public bool TryGetChannel(out SocketGuildChannel channel)
        {
            channel = null;
            foreach (SocketGuild guild in Var.client.Guilds)
            {
                channel = guild.GetChannel(Id);
                if (channel != null)
                {
                    return true;
                }
            }
            return false;
        }

        private const string JSON_NAME = "Name";
        private const string JSON_ID = "Id";
        private const string JSON_ALLOWCOMMANDS = "AllowCommands";
        private const string JSON_ALLOWSHITPOSTING = "AllowShitposting";

        public bool FromJSON(JSONObject json)
        {
            if (json.GetField(out string id_str, JSON_ID, "0"))
            {
                if (ulong.TryParse(id_str, out Id))
                {
                    json.GetField(out AllowCommands, JSON_ALLOWCOMMANDS, DEFAULT_ALLOWCOMMANDS);
                    json.GetField(out AllowShitposting, JSON_ALLOWSHITPOSTING, DEFAULT_ALLOWSHITPOSTING);
                    return true;
                }
            }
            return false;
        }

        public JSONObject ToJSON()
        {
            JSONObject result = new JSONObject();
            result.AddField(JSON_ID, Id.ToString());
            result.AddField(JSON_ALLOWCOMMANDS, AllowCommands);
            result.AddField(JSON_ALLOWSHITPOSTING, AllowShitposting);
            return result;
        }

        public override string ToString()
        {
            if (!AllowCommands)
            {
                return $"Id: `{Id}`\n" +
                    $"AllowCommands : `False`";
            }
            else
            {
                return $"Id: `{Id}`\n" +
                    $"AllowCommands : `True`\n" +
                    $"AllowShitposting : `{AllowShitposting}`";
            }
        }

        public object Clone()
        {
            return new GuildChannelInformation()
            {
                Id = this.Id,
                AllowCommands = this.AllowCommands,
                AllowShitposting = this.AllowShitposting
            };
        }
    }

    interface IJSONSerializable
    {
        bool FromJSON(JSONObject json);
        JSONObject ToJSON();
    }
}
