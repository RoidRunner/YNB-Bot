using Discord.WebSocket;
using Discord;
using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using YNBBot.NestedCommands;
using JSON;
using YNBBot.EventLogging;
using BotCoreNET;
using BotCoreNET.Helpers;
using BotCoreNET.CommandHandling;
using BotCoreNET.BotVars;

namespace YNBBot
{
    /// <summary>
    /// Handles Saving/Loading and storing of settings, aswell as some utility methods
    /// </summary>
    static class SettingsModel
    {
        #region Variables

        public static ulong AdminRole = 0;
        /// <summary>
        /// The ID of the mute role (assigned to users when the EM threshholds have been triggered)
        /// </summary>
        public static ulong MuteRole = 0;
        public static ulong GuildCaptainRole = 0;
        /// <summary>
        /// The ID of the role everybody gets to have basic access
        /// </summary>
        public static ulong GuestRole = 0;
        /// <summary>
        /// The Formatting string for the Welcoming Message. {0} is replaced with the new users mention.
        /// </summary>
        public static string welcomingMessage = "Hi {0}";

        #endregion
        #region Initialization

        #endregion
        #region JSON, Save/Load

        private const string JSON_BOTTOKEN = "BotToken";
        private const string JSON_ADMINIDS = "BotAdminIDs";
        private const string JSON_ENABLEDEBUG = "DebugEnabled";
        private const string JSON_WELCOMINGMESSAGE = "WelcomingMessage";
        private const string JSON_MODERATORROLE = "Adminrole";
        private const string JSON_BOTDEVROLE = "BotDevRole";
        private const string JSON_MINECRAFTBRANCHROLE = "MinecraftBranchRole";
        private const string JSON_MUTEROLE = "MuteRole";
        private const string JSON_PREFIX = "Prefix";
        private const string JSON_CHANNELINFOS = "ChannelInfos";
        private const string JSON_AUTOASSIGNROLEIDS = "AutoAssignRoleIds";

        internal static void SetupSettingsUpdateListener()
        {
            BotVarManager.GlobalBotVars.SubscribeToBotVarUpdateEvent(LoadSettings, "YNBsettings");
        }

        internal static void LoadSettings(ulong guildId, BotVar var)
        {
            if (var.IsGeneric)
            {
                JSONContainer json = var.Generic;

                if (json.TryGetField(JSON_ENABLEDEBUG, out JSONContainer debugSettings))
                {
                    if (debugSettings.IsArray)
                    {
                        for (int i = 0; i < debugSettings.Array.Count && i < debugLogging.Length; i++)
                        {
                            debugLogging[i] = debugSettings.Array[i].Boolean;
                        }
                    }
                }
                json.TryGetField(JSON_MODERATORROLE, out AdminRole);
                json.TryGetField(JSON_WELCOMINGMESSAGE, out welcomingMessage, welcomingMessage);
                json.TryGetField(JSON_MUTEROLE, out MuteRole);
                if (json.TryGetField(JSON_CHANNELINFOS, out JSONContainer guildChannelInfoContainer))
                {
                    GuildChannelHelper.FromJSON(guildChannelInfoContainer);
                }
                if (json.TryGetArrayField(JSON_AUTOASSIGNROLEIDS, out JSONContainer autoAssignRoles))
                {
                    foreach (JSONField idField in autoAssignRoles.Array)
                    {
                        if (idField.IsNumber && !idField.IsFloat && !idField.IsSigned)
                        {
                            EventLogger.AutoAssignRoleIds.Add(idField.Unsigned_Int64);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves all settings to appdata/locallow/Ciridium Wing Bot/Settings.json
        /// </summary>
        internal static void SaveSettings()
        {
            JSONContainer json = JSONContainer.NewObject();

            JSONContainer debugSettings = JSONContainer.NewArray();
            foreach (bool b in debugLogging)
            {
                debugSettings.Add(b);
            }
            json.TryAddField(JSON_MODERATORROLE, AdminRole);
            json.TryAddField(JSON_ENABLEDEBUG, debugSettings);
            json.TryAddField(JSON_WELCOMINGMESSAGE, welcomingMessage);
            json.TryAddField(JSON_MUTEROLE, MuteRole);
            json.TryAddField(JSON_CHANNELINFOS, GuildChannelHelper.ToJSON());

            JSONContainer autoAssignRoleIds = JSONContainer.NewArray();
            foreach (var roleId in EventLogger.AutoAssignRoleIds)
            {
                autoAssignRoleIds.Add(roleId);
            }
            json.TryAddField(JSON_AUTOASSIGNROLEIDS, autoAssignRoleIds);

            BotVarManager.GlobalBotVars.SetBotVar("YNBsettings", json);
        }

        #endregion
        #region Welcoming

        internal static async Task WelcomeNewUser(SocketGuildUser user)
        {
            if (GuildChannelHelper.TryGetChannel(GuildChannelHelper.WelcomingChannelId, out SocketTextChannel channel))
            {
                if (user.Guild.Id == channel.Guild.Id)
                {
                    if (welcomingMessage.Contains("{0}"))
                    {
                        await channel.SendEmbedAsync($"Welcome {user.Mention}!", string.Format(welcomingMessage, user.Mention));
                    }
                    else
                    {
                        await channel.SendEmbedAsync($"Welcome {user.Mention}!", welcomingMessage);
                    }
                }
            }
        }

        #endregion
        #region Debug

        public static bool[] debugLogging = new bool[Enum.GetValues(typeof(DebugCategories)).Length];

        public delegate Task Logger(LogMessage log);
        public static event Logger DebugMessage;

        /// <summary>
        /// Sends a message into the Debug Message Channel if it is defined and Debug is true
        /// </summary>
        /// <param name="message">Message to send</param>
        public static async Task SendDebugMessage(DebugCategories category, string message, string description = null)
        {
            if (DebugMessage != null)
            {
                await DebugMessage(new LogMessage(LogSeverity.Debug, category.ToString(), message));
            }
            if (debugLogging[(int)category] && GuildChannelHelper.TryGetChannel(GuildChannelHelper.DebugChannelId, out SocketTextChannel channel))
            {
                EmbedBuilder debugembed;
                if (string.IsNullOrEmpty(description))
                {
                    debugembed = new EmbedBuilder
                    {
                        Color = BotCore.EmbedColor,
                        Title = $"**[{category.ToString().ToUpper()}]**",
                        Description = message
                    };
                }
                else
                {
                    debugembed = new EmbedBuilder
                    {
                        Color = BotCore.EmbedColor,
                        Title = $"**[{category.ToString().ToUpper()}]** {message}",
                        Description = description
                    };
                }
                await channel.SendEmbedAsync(debugembed);
            }
        }

        public static async Task SendAdminCommandUsedMessage(IDMCommandContext context, Command command)
        {
            if (GuildChannelHelper.TryGetChannel(GuildChannelHelper.AdminCommandUsageLogChannelId, out SocketTextChannel channel))
            {
                EmbedBuilder debugembed = new EmbedBuilder
                {
                    Color = BotCore.EmbedColor,
                    Title = $"Admin-Only command used by {context.User.Username}#{context.User.Discriminator}",
                };
                debugembed.AddField("Command and Arguments", $"Matched Command```{command.Syntax}```Arguments```{context.Message.Content}".MaxLength(1021) + "```");
                string location;
                if (GuildCommandContext.TryConvert(context, out IGuildCommandContext guildContext))
                {
                    SocketTextChannel locationChannel = channel.Guild.GetTextChannel(guildContext.Channel.Id);
                    location = $"Guild `{guildContext.Guild.Name}` Channel {(locationChannel == null ? Markdown.InlineCodeBlock(guildContext.Channel.Name) : locationChannel.Mention)}";
                }
                else
                {
                    location = "Private Message to Bot";

                }
                debugembed.AddField("Location", location);
                await channel.SendEmbedAsync(debugembed);
            }
        }

        #endregion
        #region Access Levels

        /// <summary>
        /// Checks for the specified user having a role on a guild
        /// </summary>
        /// <param name="user">User to check</param>
        /// <param name="roleID">Role ID to check</param>
        /// <returns>true if the user has the role</returns>
        public static bool UserHasRole(SocketGuildUser user, ulong roleID)
        {
            foreach (var role in user.Roles)
            {
                if (role.Id == roleID)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

    }

    public enum DebugCategories
    {
        misc,
        timing,
        joinleave
    }
}
