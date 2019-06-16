using Discord;
using Discord.Rest;
using Discord.WebSocket;
using JSON;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YNBBot
{
    static class GuildChannelHelper
    {
        #region Fields

        /// <summary>
        /// Id of the channel assigned as debug logging channel
        /// </summary>
        public static ulong DebugChannelId;
        /// <summary>
        /// Id of the channel assigned for welcoming new users
        /// </summary>
        public static ulong WelcomingChannelId;
        /// <summary>
        /// Id of the channel used to log admin command usage
        /// </summary>
        public static ulong AdminCommandUsageLogChannelId;
        /// <summary>
        /// Id of the channel used to send admin notifications to
        /// </summary>
        public static ulong AdminNotificationChannelId;
        /// <summary>
        /// Id of the channel used to send interactive messages to
        /// </summary>
        public static ulong InteractiveMessagesChannelId;
        /// <summary>
        /// Id of the category all guilds are placed below
        /// </summary>
        public static ulong GuildCategoryId;

        private const string JSON_DEBUGCHANNELID = "DebugChannelId";
        private const string JSON_WELCOMINGCHANNELID = "WelcomingChannelId";
        private const string JSON_ADMINCOMMANDUSAGELOGCHANNELID = "CommandLogChannelId";
        private const string JSON_ADMINNOTIFICATIONCHANNELID = "NotificationChannelId";
        private const string JSON_INTERACTIVEMESSAGECHANNELID = "InteractiveChannelId";
        private const string JSON_GUILDCATEGORYID = "GuildCategoryId";
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
        public static void FromJSON(JSONContainer json)
        {
            channelConfigs.Clear();

            json.TryGetField(JSON_DEBUGCHANNELID, out DebugChannelId);
            json.TryGetField(JSON_WELCOMINGCHANNELID, out WelcomingChannelId);
            json.TryGetField(JSON_ADMINCOMMANDUSAGELOGCHANNELID, out AdminCommandUsageLogChannelId);
            json.TryGetField(JSON_ADMINNOTIFICATIONCHANNELID, out AdminNotificationChannelId);
            json.TryGetField(JSON_INTERACTIVEMESSAGECHANNELID, out InteractiveMessagesChannelId);
            json.TryGetField(JSON_GUILDCATEGORYID, out GuildCategoryId);

            if (json.TryGetField(JSON_CHANNELINFOS, out IReadOnlyList<JSONField> channelInfos))
            {
                foreach (JSONField channelInfo in channelInfos)
                {
                    if (channelInfo.IsObject)
                    {
                        GuildChannelConfiguration info = new GuildChannelConfiguration();
                        if (info.FromJSON(channelInfo.Container))
                        {
                            channelConfigs.Add(info.Id, info);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Converts currently stored configs and ids into a json data object
        /// </summary>
        /// <returns>json data</returns>
        public static JSONContainer ToJSON()
        {
            JSONContainer json = JSONContainer.NewObject();

            json.TryAddField(JSON_DEBUGCHANNELID, DebugChannelId);
            json.TryAddField(JSON_WELCOMINGCHANNELID, WelcomingChannelId);
            json.TryAddField(JSON_ADMINCOMMANDUSAGELOGCHANNELID, AdminCommandUsageLogChannelId);
            json.TryAddField(JSON_ADMINNOTIFICATIONCHANNELID, AdminNotificationChannelId);
            json.TryAddField(JSON_INTERACTIVEMESSAGECHANNELID, InteractiveMessagesChannelId);
            json.TryAddField(JSON_GUILDCATEGORYID, GuildCategoryId);

            JSONContainer channelInfoArray = JSONContainer.NewArray();
            foreach (GuildChannelConfiguration channelInfo in channelConfigs.Values)
            {
                channelInfoArray.Add(channelInfo.ToJSON());
            }
            json.TryAddField(JSON_CHANNELINFOS, channelInfoArray);

            return json;
        }

        #endregion
        #region Message Sending

        public static async Task<RestUserMessage> SendMessage(ulong channelId, string content = null, EmbedBuilder embed = null, string embedTitle = null, string embedDescription = null, bool useErrorColor = false)
        {
            return await SendMessage(channelId, useErrorColor ? Var.ERRORCOLOR : Var.BOTCOLOR, content, embed, embedTitle, embedDescription);
        }

        public static async Task<RestUserMessage> SendMessage(ulong channelId, Color color, string content = null, EmbedBuilder embed = null, string embedTitle = null, string embedDescription = null)
        {
            if (TryGetChannel(channelId, out SocketTextChannel channel))
            {
                if (embed == null && (embedDescription != null || embedTitle != null))
                {
                    embed = new EmbedBuilder()
                    {
                        Color = color
                    };
                    if (embedTitle != null)
                    {
                        embed.Title = embedTitle;
                    }
                    if (embedDescription != null)
                    {
                        embed.Description = embedDescription;
                    }
                }
                if (content != null || embed != null)
                {
                    return await channel.SendMessageAsync(content, embed: embed.Build());
                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        public static async Task<RestUserMessage> SendExceptionNotification(Exception e, string context)
        {
            bool botDevRoleFound = Var.client.TryGetRole(SettingsModel.BotDevRole, out SocketRole botDevRole);
            return await SendMessage(DebugChannelId, content: $"{(botDevRoleFound ? botDevRole.Mention : "")} {context}", embed: Macros.EmbedFromException(e), useErrorColor: true);
        }

        #endregion
    }

    /// <summary>
    /// Stores configuration of command behaviour for one guild channel
    /// </summary>
    public class GuildChannelConfiguration : IJSONSerializable, ICloneable
    {
        #region Constants, Fields, Properties

        public const bool DEFAULT_ALLOWCOMMANDS = false;
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

        private const string JSON_ID = "Id";
        private const string JSON_ALLOWCOMMANDS = "AllowCommands";
        private const string JSON_ALLOWSHITPOSTING = "AllowShitposting";

        public bool FromJSON(JSONContainer json)
        {
            if (json.TryGetField(JSON_ID, out Id))
            {
                json.TryGetField(JSON_ALLOWCOMMANDS, out AllowCommands, DEFAULT_ALLOWCOMMANDS);
                json.TryGetField(JSON_ALLOWSHITPOSTING, out AllowShitposting, DEFAULT_ALLOWSHITPOSTING);
                return true;
            }
            return false;
        }

        public JSONContainer ToJSON()
        {
            JSONContainer result = JSONContainer.NewObject();
            result.TryAddField(JSON_ID, Id);
            result.TryAddField(JSON_ALLOWCOMMANDS, AllowCommands);
            result.TryAddField(JSON_ALLOWSHITPOSTING, AllowShitposting);
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
        bool FromJSON(JSONContainer json);
        JSONContainer ToJSON();
    }
}
