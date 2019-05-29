using Discord.WebSocket;
using Discord;
using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using YNBBot.NestedCommands;

namespace YNBBot
{
    /// <summary>
    /// Handles Saving/Loading and storing of settings, aswell as some utility methods
    /// </summary>
    static class SettingsModel
    {
        #region Variables
        /// <summary>
        /// The bot token used to log into discord
        /// </summary>
        internal static string token;
        /// <summary>
        /// A list containing all Bot Admin IDs
        /// </summary>
        public static List<ulong> botAdminIDs;
        /// <summary>
        /// The ID of the moderator role
        /// </summary>
        public static ulong AdminRole = 0;
        /// <summary>
        /// The Id of the minecraft branch role
        /// </summary>
        public static ulong MinecraftBranchRole = 0;
        /// <summary>
        /// The ID of the bot dev role (pinging on error messages)
        /// </summary>
        public static ulong BotDevRole = 0;
        /// <summary>
        /// The Formatting string for the Welcoming Message. {0} is replaced with the new users mention.
        /// </summary>
        public static string welcomingMessage = "Hi {0}";

        #endregion
        #region Initialization

        static SettingsModel()
        {
            botAdminIDs = new List<ulong>();
        }

        /// <summary>
        /// Initializes variables, loads settings and checks if loading was successful
        /// </summary>
        /// <param name="nclient"></param>
        /// <returns>False if loading of the critical variables (bottoken, botadminIDs) fails</returns>
        public static async Task<bool> LoadSettingsAndCheckToken(DiscordSocketClient nclient)
        {
            await loadSettings();
            return token != null && botAdminIDs.Count > 0;
        }

        #endregion
        #region JSON, Save/Load

        private const string JSON_BOTTOKEN = "BotToken";
        private const string JSON_ADMINIDS = "BotAdminIDs";
        private const string JSON_ENABLEDEBUG = "DebugEnabled";
        private const string JSON_WELCOMINGMESSAGE = "WelcomingMessage";
        private const string JSON_MODERATORROLE = "Adminrole";
        private const string JSON_BOTDEVROLE = "BotDevRole";
        private const string JSON_MINECRAFTBRANCHROLE = "MinecraftBranchRole";
        private const string JSON_PREFIX = "Prefix";
        private const string JSON_CHANNELINFOS = "ChannelInfos";

        /// <summary>
        /// Loads and applies Settings from appdata/locallow/Ciridium Wing Bot/Settings.json
        /// </summary>
        /// <returns></returns>
        private static async Task loadSettings()
        {
            LoadFileOperation operation = await ResourcesModel.LoadToJSONObject(ResourcesModel.SettingsFilePath);
            if (operation.Success)
            {
                JSONObject json = operation.Result;
                if (json.GetField(ref token, JSON_BOTTOKEN) && json.HasFields(new string[] { JSON_ADMINIDS }))
                {
                    JSONObject botadmins = json[JSON_ADMINIDS];
                    if (botadmins.IsArray && botadmins.list != null)
                    {
                        foreach (var admin in botadmins.list)
                        {
                            if (ulong.TryParse(admin.str, out ulong nID))
                            {
                                botAdminIDs.Add(nID);
                            }
                        }
                    }
                    if (json.HasField(JSON_ENABLEDEBUG))
                    {
                        JSONObject debugSettings = json[JSON_ENABLEDEBUG];
                        if (debugSettings.IsArray)
                        {
                            for (int i = 0; i < debugSettings.list.Count && i < debugLogging.Length; i++)
                            {
                                debugLogging[i] = debugSettings.list[i].b;
                            }
                        }
                    }
                    string id = "";
                    if (json.GetField(out string welcomingMessage_str, JSON_WELCOMINGMESSAGE, null))
                    {
                        welcomingMessage = JSONObject.GetOriginalFormat(welcomingMessage_str);
                    }
                    if (json.GetField(ref id, JSON_MODERATORROLE))
                    {
                        ulong.TryParse(id, out AdminRole);
                    }
                    if (json.GetField(ref id, JSON_BOTDEVROLE))
                    {
                        ulong.TryParse(id, out BotDevRole);
                    }
                    json.GetField(out MinecraftBranchRole, JSON_MINECRAFTBRANCHROLE);
                    string prefix_str = string.Empty;
                    if (json.GetField(ref prefix_str, JSON_PREFIX))
                    {
                        if (prefix_str.Length > 0)
                        {
                            CommandHandler.Prefix = prefix_str[0];
                        }
                    }
                    JSONObject guildChannelInfos = json[JSON_CHANNELINFOS];
                    if (guildChannelInfos != null)
                    {
                        GuildChannelHelper.FromJSON(guildChannelInfos);
                    }
                }
            }
        }

        /// <summary>
        /// Saves all settings to appdata/locallow/Ciridium Wing Bot/Settings.json
        /// </summary>
        internal static async Task SaveSettings()
        {
            JSONObject json = new JSONObject();

            json.AddField(JSON_BOTTOKEN, token);
            JSONObject adminIDs = new JSONObject();
            foreach (var adminID in botAdminIDs)
            {
                adminIDs.Add(adminID.ToString());
            }
            json.AddField(JSON_ADMINIDS, adminIDs);
            JSONObject debugSettings = new JSONObject();
            foreach (bool b in debugLogging)
            {
                debugSettings.Add(b);
            }
            json.AddField(JSON_ENABLEDEBUG, debugSettings);
            json.AddField(JSON_WELCOMINGMESSAGE, JSONObject.GetSafelyFormattedString(welcomingMessage));
            json.AddField(JSON_MODERATORROLE, AdminRole.ToString());
            json.AddField(JSON_BOTDEVROLE, BotDevRole.ToString());
            json.AddField(JSON_MINECRAFTBRANCHROLE, MinecraftBranchRole);
            json.AddField(JSON_PREFIX, CommandHandler.Prefix.ToString());
            json.AddField(JSON_CHANNELINFOS, GuildChannelHelper.ToJSON());

            await ResourcesModel.WriteJSONObjectToFile(ResourcesModel.SettingsFilePath, json);
        }

        #endregion
        #region Welcoming

        internal static async Task WelcomeNewUser(SocketGuildUser user)
        {
            if (GuildChannelHelper.TryGetChannel(GuildChannelHelper.WelcomingChannelId, out SocketTextChannel channel))
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

        #endregion
        #region Debug

        public static bool[] debugLogging = new bool[Enum.GetValues(typeof(DebugCategories)).Length];

        public delegate Task Logger(LogMessage log);
        public static event Logger DebugMessage;

        /// <summary>
        /// Sends a message into the Debug Message Channel if it is defined and Debug is true
        /// </summary>
        /// <param name="message">Message to send</param>
        public static async Task SendDebugMessage(string message, DebugCategories category)
        {
            if (DebugMessage != null)
            {
                await DebugMessage(new LogMessage(LogSeverity.Debug, category.ToString(), message));
            }
            if (debugLogging[(int)category] && GuildChannelHelper.TryGetChannel(GuildChannelHelper.DebugChannelId, out SocketTextChannel channel))
            {
                EmbedBuilder debugembed = new EmbedBuilder
                {
                    Color = Var.BOTCOLOR,
                    Title = string.Format("**__Debug: {0}__**", category.ToString().ToUpper()),
                    Description = message
                };
                await channel.SendEmbedAsync(debugembed);
            }
        }

        public static async Task SendAdminCommandUsedMessage(CommandContext context, Command command)
        {
            if (GuildChannelHelper.TryGetChannel(GuildChannelHelper.AdminCommandUsageLogChannelId, out SocketTextChannel channel))
            {
                EmbedBuilder debugembed = new EmbedBuilder
                {
                    Color = Var.BOTCOLOR,
                    Title = $"Admin-Only command used by {context.User.Username}#{context.User.Discriminator}",
                };
                debugembed.AddField("Command and Arguments", $"Matched Command```{command.PrefixIdentifier}```Arguments```{context.Message.Content}".MaxLength(1021) + "```");
                string location;
                if (GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext))
                {
                    SocketTextChannel locationChannel = channel.Guild.GetTextChannel(guildContext.Channel.Id);
                    location = $"Guild `{guildContext.Guild.Name}` Channel {(locationChannel == null ? Macros.InlineCodeBlock(guildContext.Channel.Name) : locationChannel.Mention)}";
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
        /// Checks if the user is listed as bot admin
        /// </summary>
        /// <param name="userID">User ID to check</param>
        /// <returns>true if the user is a bot admin</returns>
        public static bool UserIsBotAdmin(ulong userID)
        {
            return botAdminIDs.Contains(userID);
        }

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

        /// <summary>
        /// Checks a users access level
        /// </summary>
        /// <param name="user">User to check</param>
        /// <returns>the users access level</returns>
        public static AccessLevel GetUserAccessLevel(SocketGuildUser user)
        {
            if (UserIsBotAdmin(user.Id))
            {
                return AccessLevel.BotAdmin;
            }
            AccessLevel result = AccessLevel.Basic;
            foreach (var role in user.Roles)
            {
                if (role.Id == AdminRole && result < AccessLevel.Admin)
                {
                    result = AccessLevel.Admin;
                }
            }
            return result;
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
