using Discord.WebSocket;
using System;
using System.Collections.Generic;

namespace YNBBot
{
    static class GuildChannelHelper
    {
        #region Fields

        /// <summary>
        /// Id of the channel assigned as debug logging channel
        /// </summary>
        public static ulong DebugChannelId { get; set; }
        /// <summary>
        /// Id of the channel assigned for welcoming new users
        /// </summary>
        public static ulong WelcomingChannelId { get; set; }

        private const string JSON_DEBUGCHANNELID = "DebugChannelId";
        private const string JSON_WELCOMINGCHANNELID = "WelcomingChannelId";
        private const string JSON_CHANNELINFOS = "ChannelInfos";
        private static Dictionary<ulong, GuildChannelConfiguration> channelConfigs = new Dictionary<ulong, GuildChannelConfiguration>();

        #endregion
        #region Channel Configs

        /// <summary>
        /// Gets channel config for a given socketguildchannel. If no channel config for that channel is stored, default is returned instead
        /// </summary>
        /// <param name="channel">A socketguild channel to retrieve the channel config for</param>
        /// <returns>Specific channel config or default</returns>
        public static GuildChannelConfiguration GetChannelConfigOrDefault(SocketGuildChannel channel)
        {
            if (channelConfigs.TryGetValue(channel.Id, out GuildChannelConfiguration channelConfig))
            {
                return channelConfig;
            }
            else
            {
                return new GuildChannelConfiguration(channel.Id);
            }
        }

        /// <summary>
        /// Attempts to retrieve specific channel info for a given channel id
        /// </summary>
        /// <param name="channelId">ulong id of the channel</param>
        /// <param name="channelConfig">the resulting channel info</param>
        /// <returns>True if specific channel info was found</returns>
        public static bool TryGetChannelConfig(ulong channelId, out GuildChannelConfiguration channelConfig)
        {
            return channelConfigs.TryGetValue(channelId, out channelConfig);
        }

        /// <summary>
        /// Sets the channel config for a given channel
        /// </summary>
        /// <param name="channelConfig"></param>
        public static void SetChannelConfig(GuildChannelConfiguration channelConfig)
        {
            if (channelConfigs.ContainsKey(channelConfig.Id))
            {
                if (channelConfig.IsDefault)
                {
                    channelConfigs.Remove(channelConfig.Id);
                }
                else
                {
                    channelConfigs[channelConfig.Id] = channelConfig;
                }
            }
            else
            {
                if (!channelConfig.IsDefault)
                {
                    channelConfigs.Add(channelConfig.Id, channelConfig);
                }
            }
        }

        #endregion
        #region Guild Channels

        /// <summary>
        /// Searches all guild channels available to the client for a channel Id
        /// </summary>
        /// <param name="channelId">ulong id of the channel to search for</param>
        /// <param name="channel">The resulting channel</param>
        /// <returns>True if a channel matching the id was found</returns>
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

        /// <summary>
        /// Searches all guild channels available to the client for a channel id and tries converting it to a type
        /// </summary>
        /// <typeparam name="T">The type to try converting the channel to</typeparam>
        /// <param name="channelId">ulong id of the channel to search for</param>
        /// <param name="channel">The resulting channel</param>
        /// <returns>True if of type T matching the id was found</returns>
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

        #endregion
        #region JSON

        /// <summary>
        /// Initiates the GuildChannelHelpers stored configs and ids from a json object
        /// </summary>
        /// <param name="json">json data</param>
        public static void FromJSON(JSONObject json)
        {
            channelConfigs.Clear();
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
                    GuildChannelConfiguration info = new GuildChannelConfiguration();
                    if (info.FromJSON(channelInfoArray))
                    {
                        channelConfigs.Add(info.Id, info);
                    }
                }
            }
        }

        /// <summary>
        /// Converts currently stored configs and ids into a json data object
        /// </summary>
        /// <returns>json data</returns>
        public static JSONObject ToJSON()
        {
            JSONObject json = new JSONObject();

            json.AddField(JSON_DEBUGCHANNELID, DebugChannelId.ToString());
            json.AddField(JSON_WELCOMINGCHANNELID, WelcomingChannelId.ToString());

            JSONObject channelInfoArray = new JSONObject();
            foreach (GuildChannelConfiguration channelInfo in channelConfigs.Values)
            {
                channelInfoArray.Add(channelInfo.ToJSON());
            }
            json.AddField(JSON_CHANNELINFOS, channelInfoArray);

            return json;
        }

        #endregion
    }

    /// <summary>
    /// Stores configuration of command behaviour for one guild channel
    /// </summary>
    public class GuildChannelConfiguration : IJSONSerializable, ICloneable
    {
        #region Constants, Fields, Properties

        public const bool DEFAULT_ALLOWCOMMANDS = true;
        public const bool DEFAULT_ALLOWSHITPOSTING = false;

        /// <summary>
        /// ulong Id of the channel this object stores the configuration of
        /// </summary>
        public ulong Id = 0;
        /// <summary>
        /// Wether command execution is allowed in this channel or not
        /// </summary>
        public bool AllowCommands = DEFAULT_ALLOWCOMMANDS;
        /// <summary>
        /// Wether shitposting command execution is allowed or not
        /// </summary>
        public bool AllowShitposting = DEFAULT_ALLOWSHITPOSTING;

        public bool IsDebugChannel { get { return Id == GuildChannelHelper.DebugChannelId; } }
        public bool IsWelcomingChannel { get { return Id == GuildChannelHelper.WelcomingChannelId; } }

        /// <summary>
        /// True, if the channel configuration matches default configuration, which means it does not have to be stored
        /// </summary>
        public bool IsDefault
        {
            get
            {
                return AllowCommands == DEFAULT_ALLOWCOMMANDS && AllowShitposting == DEFAULT_ALLOWSHITPOSTING;
            }
        }

        #endregion
        #region Constructors

        /// <summary>
        /// Empty constructor initializing with default settings
        /// </summary>
        public GuildChannelConfiguration()
        {

        }

        /// <summary>
        /// Constructor with Id and settings parameters
        /// </summary>
        /// <param name="id">Sets the id of the channel this object stores the config for</param>
        /// <param name="allowCommands"></param>
        /// <param name="allowShitposting"></param>
        public GuildChannelConfiguration(ulong id, bool allowCommands = DEFAULT_ALLOWCOMMANDS, bool allowShitposting = DEFAULT_ALLOWSHITPOSTING)
        {
            Id = id;
            AllowCommands = allowCommands;
            AllowShitposting = allowShitposting;
        }

        #endregion
        #region JSON

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

        #endregion
        #region Overrides, Implements

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
            return new GuildChannelConfiguration()
            {
                Id = this.Id,
                AllowCommands = this.AllowCommands,
                AllowShitposting = this.AllowShitposting
            };
        }

        #endregion
    }

    interface IJSONSerializable
    {
        bool FromJSON(JSONObject json);
        JSONObject ToJSON();
    }
}
