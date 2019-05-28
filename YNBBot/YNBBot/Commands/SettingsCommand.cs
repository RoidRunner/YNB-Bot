//using Discord.Commands;
//using Discord.WebSocket;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace YNBBot
//{
//    /// <summary>
//    /// Handles all commands that change settings, such as default channels, welcoming messages.
//    /// </summary>
//    [Obsolete]
//    class SettingsCommands
//    {
//        public SettingsCommands()
//        {
//            // settings
//            CommandService.AddCommand(new CommandOld(new CommandKeys(CMDKEYS_SETTINGS), HandleCommand, AccessLevel.Admin, CMDSUMMARY_SETTINGS, CMDSYNTAX_SETTINGS, CommandOld.NO_ARGUMENTS));
//            // settings debug
//            CommandService.AddCommand(new CommandOld(new CommandKeys(CMDKEYS_SETTINGS_DEBUG, 4, 4), HandleDebugLoggingCommand, AccessLevel.Admin, CMDSUMMARY_SETTINGS_DEBUG, CMDSYNTAX_SETTINGS_DEBUG, CMDARGS_SETTINGS_DEBUG));
//            // settings channel
//            CommandService.AddCommand(new CommandOld(new CommandKeys(CMDKEYS_SETTINGS_CHANNEL, 4, 4), HandleDefaultChannelCommand, AccessLevel.Admin, CMDSUMMARY_SETTINGS_CHANNEL, CMDSYNTAX_SETTINGS_CHANNEL, CMDARGS_SETTINGS_CHANNEL));
//            // settings role
//            CommandService.AddCommand(new CommandOld(new CommandKeys(CMDKEYS_SETTINGS_ROLE, 4, 4), HandleSetRoleCommand, AccessLevel.Admin, CMDSUMMARY_SETTINGS_ROLE, CMDSYNTAX_SETTINGS_ROLE, CMDARGS_SETTINGS_ROLE));
//            // settings setmissionnumber
//            CommandService.AddCommand(new CommandOld(new CommandKeys(CMDKEYS_SETTINGS_PREFIX, 3, 3), HandlePrefixCommand, AccessLevel.Admin, CMDKEYS_SETTINGS_PREFIX, CMDSYNTAX_SETTINGS_PREFIX, CMDARGS_SETTINGS_PREFIX));
//            // settings setjoinmsg
//            CommandService.AddCommand(new CommandOld(new CommandKeys(CMDKEYS_SETTINGS_SETJOINMSG, 3, 1000), HandleWelcomingMessageCommand, AccessLevel.Admin, CMDSUMMARY_SETTINGS_SETJOINMSG, CMDSYNTAX_SETTINGS_SETJOINMSG, CMDARGS_SETTINGS_SETJOINMSG));
//        }

//        #region /settings

//        private const string CMDKEYS_SETTINGS = "settings";
//        private const string CMDSYNTAX_SETTINGS = "settings";
//        private const string CMDSUMMARY_SETTINGS = "Lists current settings";

//        public async Task HandleCommand(SocketCommandContext context)
//        {
//            await context.Channel.SendEmbedAsync(SettingsModel.DebugSettingsMessage);
//        }

//        #endregion
//        #region /settings debug

//        private const string CMDKEYS_SETTINGS_DEBUG = "settings debug";
//        private const string CMDSYNTAX_SETTINGS_DEBUG = "settings debug";
//        private const string CMDSUMMARY_SETTINGS_DEBUG = "Enables/Disables debug messages for a debug category";
//        private const string CMDARGS_SETTINGS_DEBUG =
//                "    <Category>\n" +
//                "Available debug categories are: 'misc', 'timing' & 'joinleave'\n" +
//                "    <Enable>\n" +
//                "Enable the category by setting 'true', disable by 'false'";

//        private static async Task HandleDebugLoggingCommand(CommandContextOld context)
//        {
//            string message;
//            bool error = false;
//            if (Enum.TryParse(context.Args[2], out DebugCategories debugcategory))
//            {
//                bool oldsetting = SettingsModel.debugLogging[(int)debugcategory];
//                if (bool.TryParse(context.Args[3], out SettingsModel.debugLogging[(int)debugcategory]))
//                {
//                    message = string.Format("{0} debug logging for '{1}'", SettingsModel.debugLogging[(int)debugcategory] ? "Enabled" : "Disabled", context.Args[2].ToUpper());
//                }
//                else
//                {
//                    error = true;
//                    SettingsModel.debugLogging[(int)debugcategory] = oldsetting;
//                    message = "Do you want it turned on or off? I am confused";
//                }
//            }
//            else
//            {
//                error = true;
//                message = "I don't know that debug logging category";
//            }
//            if (!error)
//            {
//                await SettingsModel.SaveSettings();
//            }
//            await context.Channel.SendEmbedAsync(message, error);
//        }

//        #endregion
//        #region /settings role

//        private const string CMDKEYS_SETTINGS_ROLE = "settings role";
//        private const string CMDSYNTAX_SETTINGS_ROLE = "settings role <AccessLevel> <@Role>";
//        private const string CMDSUMMARY_SETTINGS_ROLE = "Sets the pilot/moderator role used to handle access to bot commands";
//        private const string CMDARGS_SETTINGS_ROLE =
//                "    <AccessLevel>\n" +
//                "Which of the access levels you want to assign a role to. Available are 'botdev' & 'admin'\n" +
//                "    <@Role>\n" +
//                "Specify the role that you want to assign, either by mention or as uInt64 Id";

//        private async Task HandleSetRoleCommand(CommandContextOld context)
//        {
//            ulong? roleId = null;
//            string message = "";
//            bool error = false;

//            if (context.Message.MentionedRoles.Count > 0)
//            {
//                roleId = new List<SocketRole>(context.Message.MentionedRoles)[0].Id;
//            }
//            else if (ulong.TryParse(context.Args[3], out ulong parsedRoleId))
//            {
//                roleId = parsedRoleId;
//            }
//            else
//            {
//                message = "Fourth argument must contain either a role mention or a RoleId!";
//                error = true;
//            }

//            if (roleId != null)
//            {
//                switch (context.Args[2])
//                {
//                    case "admin":
//                        SettingsModel.AdminRole = (ulong)roleId;
//                        message = "Admin Role set to " + context.Guild.GetRole((ulong)roleId).Mention;
//                        break;
//                    case "botdev":
//                        SettingsModel.BotDevRole = (ulong)roleId;
//                        message = "BotDev Role set to " + context.Guild.GetRole((ulong)roleId).Mention;
//                        break;
//                    default:
//                        error = true;
//                        message = "Unknown Role Identifier";
//                        break;
//                }
//            }

//            if (!error)
//            {
//                await SettingsModel.SaveSettings();
//            }
//            await context.Channel.SendEmbedAsync(message, error);
//        }

//        #endregion
//        #region /settings channel

//        private const string CMDKEYS_SETTINGS_CHANNEL = "settings channel";
//        private const string CMDSYNTAX_SETTINGS_CHANNEL = "settings channel <Channel> <ChannelId>";
//        private const string CMDSUMMARY_SETTINGS_CHANNEL = "Sets the channel used for debug, welcoming and the mission channel category";
//        private const string CMDARGS_SETTINGS_CHANNEL =
//                "    <Channel>\n" +
//                "Which default channel setting you wish to override. Available are 'debug', 'welcoming' & 'missioncategory', and 'allowshitposting', 'denyshitposting' for shitposting channel management\n" +
//                "    <ChannelId>\n" +
//                "Specify the channel you want to assign either by mention, as uInt64 Id or by the keyword 'this'";

//        /// <summary>
//        /// handles the "/settings channel" command
//        /// </summary>
//        /// <param name="context"></param>
//        /// <param name="args"></param>
//        /// <returns></returns>
//        private static async Task HandleDefaultChannelCommand(CommandContextOld context)
//        {
//            string message = "";
//            bool error = false;
//            ulong? channelId = null;
//            if (ulong.TryParse(context.Args[3], out ulong Id))
//            {
//                channelId = Id;
//            }
//            else if (context.Message.MentionedChannels.Count > 0)
//            {
//                channelId = new List<SocketGuildChannel>(context.Message.MentionedChannels)[0].Id;
//            }
//            else if (context.Args[3].Equals("this"))
//            {
//                channelId = context.Channel.Id;
//            }
//            else
//            {
//                error = true;
//                message = "Cannot Parse the supplied Id as an uInt64 Value!";
//            }

//            if (channelId != null)
//            {
//                switch (context.Args[2])
//                {
//                    case "debug":
//                        SettingsModel.DebugMessageChannelId = (ulong)channelId;
//                        await SettingsModel.SaveSettings();
//                        message = "Debug channel successfully set to " + context.Guild.GetChannel((ulong)channelId)?.Name;
//                        break;
//                    case "welcoming":
//                        SettingsModel.WelcomeMessageChannelId = (ulong)channelId;
//                        await SettingsModel.SaveSettings();
//                        message = "Welcoming channel successfully set to " + context.Guild.GetChannel((ulong)channelId)?.Name;
//                        break;
//                    case "allowshitposting":
//                        if (!SettingsModel.ShitpostingEnabledChannels.Contains((ulong)channelId))
//                        {
//                            SettingsModel.ShitpostingEnabledChannels.Add((ulong)channelId);
//                            await SettingsModel.SaveSettings();
//                            message = string.Format("{0} is now a fun-enabled zone!", context.Guild.GetChannel((ulong)channelId)?.Name);
//                        }
//                        else
//                        {
//                            message = "Shitposting is already rocking high in " + context.Guild.GetChannel((ulong)channelId)?.Name;
//                        }
//                        break;
//                    case "denyshitposting":
//                        if (SettingsModel.ShitpostingEnabledChannels.Contains((ulong)channelId))
//                        {
//                            SettingsModel.ShitpostingEnabledChannels.Remove((ulong)channelId);
//                            await SettingsModel.SaveSettings();
//                            message = string.Format("{0} is now a no-fun-allowed zone!", context.Guild.GetChannel((ulong)channelId)?.Name);
//                        }
//                        else
//                        {
//                            message = "Nobody was shitposting there before anyways";
//                        }
//                        break;
//                    default:
//                        error = true;
//                        message = "I don't know that default channel!";
//                        break;
//                }
//            }

//            if (!error)
//            {
//                await SettingsModel.SaveSettings();
//            }
//            await context.Channel.SendEmbedAsync(message, error);
//        }

//        #endregion
//        #region /settings setjoinmsg
//#if WELCOMING_MESSAGES

//        private const string CMDKEYS_SETTINGS_SETJOINMSG = "settings setjoinmsg";
//        private const string CMDSYNTAX_SETTINGS_SETJOINMSG = "settings setjoinmsg {<Words>}";
//        private const string CMDSUMMARY_SETTINGS_SETJOINMSG = "Sets the welcoming message";
//        private const string CMDARGS_SETTINGS_SETJOINMSG =
//                "    {<Words>}\n" +
//                "All words following the initial arguments will be the new join message. Insert '{0}' wherever you want the new user pinged!";

//        private static async Task HandleWelcomingMessageCommand(CommandContextOld context)
//        {
//            string nwelcomingMessage = context.Message.Content.Substring(21);

//            if (!nwelcomingMessage.Contains("{0}"))
//            {
//                await context.Channel.SendEmbedAsync("You need to specify locations for:```" +
//                    "{0} : User that joined\n" +
//                    "```", true);
//            }
//            else
//            {
//                SettingsModel.welcomingMessage = nwelcomingMessage;
//                SocketTextChannel channel = context.Guild.GetTextChannel(SettingsModel.WelcomeMessageChannelId);
//                await SettingsModel.SaveSettings();
//                await context.Channel.SendEmbedAsync(string.Format("Welcoming Message updated successfully. I welcomed you in the welcoming channel: {0}", channel.Mention));
//                //await SettingsModel.WelcomeNewUser(context.User);
//            }
//        }

//#endif
//        #endregion
//        #region /settings prefix

//        private const string CMDKEYS_SETTINGS_PREFIX = "settings prefix";
//        private const string CMDSYNTAX_SETTINGS_PREFIX = "settings prefix <Number>";
//        private const string CMDSUMMARY_SETTINGS_PREFIX = "Sets the command prefix";
//        private const string CMDARGS_SETTINGS_PREFIX =
//                "    <Character>\n" +
//                "Specify the character identifying commands";

//        private static async Task HandlePrefixCommand(CommandContextOld context)
//        {
//            CommandService.Prefix = context.Args[2][0];
//            await SettingsModel.SaveSettings();
//            await context.Channel.SendEmbedAsync(string.Format("Commandprefix updated to `{0}`", CommandService.Prefix));
//        }

//        #endregion
//    }
//}
