#if OLDCOMMANDS

using BotCoreNET;
using BotCoreNET.CommandHandling;
using BotCoreNET.Helpers;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YNBBot.EventLogging;

namespace YNBBot.NestedCommands
{
    // Config

    #region detect

    class DetectConfigCommand : Command
    {
        public const string SUMMARY = "Lists current configuration";
        public const string REMARKS = default;
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.xgjci6e0iq7a";
        public static readonly Argument[] ARGS = new Argument[] { };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public DetectConfigCommand(string identifier) : base(identifier, OverriddenMethod.None, OverriddenMethod.BasicAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            GuildChannelHelper.TryGetChannel(GuildChannelHelper.DebugChannelId, out SocketTextChannel debugChannel);
            GuildChannelHelper.TryGetChannel(GuildChannelHelper.WelcomingChannelId, out SocketTextChannel welcomingChannel);
            GuildChannelHelper.TryGetChannel(GuildChannelHelper.AdminCommandUsageLogChannelId, out SocketTextChannel adminCommandUsageLogging);
            GuildChannelHelper.TryGetChannel(GuildChannelHelper.AdminNotificationChannelId, out SocketTextChannel adminNotificationChannel);
            GuildChannelHelper.TryGetChannel(GuildChannelHelper.InteractiveMessagesChannelId, out SocketTextChannel interactiveMessagesChannel);
            SocketRole adminRole = null;
            SocketRole botNotifications = null;
            SocketRole minecraftBranch = null;
            SocketRole mute = null;

            if (GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext))
            {
                adminRole = guildContext.Guild.GetRole(SettingsModel.AdminRole);
                botNotifications = guildContext.Guild.GetRole(SettingsModel.BotDevRole);
                minecraftBranch = guildContext.Guild.GetRole(SettingsModel.MinecraftBranchRole);
                mute = guildContext.Guild.GetRole(SettingsModel.MuteRole);
            }

            EmbedBuilder embed = new EmbedBuilder()
            {
                Title = "Current Settings",
                Color = BotCore.EmbedColor,
                Description = $"YNB Bot {Var.VERSION}"
            };
            StringBuilder debugLogging = new StringBuilder("Logging Channel: ");
            if (debugChannel == null)
            {
                debugLogging.AppendLine(Markdown.InlineCodeBlock(GuildChannelHelper.DebugChannelId));
            }
            else
            {
                debugLogging.AppendLine(debugChannel.Mention);
            }
            for (int i = 0; i < SettingsModel.debugLogging.Length; i++)
            {
                bool option = SettingsModel.debugLogging[i];
                debugLogging.AppendLine($"{(DebugCategories)i}: { (option ? "**enabled**" : "disabled") }");
            }
            embed.AddField("Debug Logging", debugLogging);
            embed.AddField("Channels", $"Welcoming: { (welcomingChannel == null ? Markdown.InlineCodeBlock(GuildChannelHelper.WelcomingChannelId) : welcomingChannel.Mention) }\n" +
                $"Interactive Messages: {(interactiveMessagesChannel == null ? Markdown.InlineCodeBlock(GuildChannelHelper.InteractiveMessagesChannelId) : interactiveMessagesChannel.Mention)}\n" +
                $"Admin Command Usage Logging: {(adminCommandUsageLogging == null ? Markdown.InlineCodeBlock(GuildChannelHelper.AdminCommandUsageLogChannelId) : adminCommandUsageLogging.Mention)}\n" +
                $"Admin Notifications: {(adminNotificationChannel == null ? Markdown.InlineCodeBlock(GuildChannelHelper.AdminNotificationChannelId) : adminNotificationChannel.Mention)}");

            embed.AddField("Roles", $"Admin Role: { (adminRole == null ? Markdown.InlineCodeBlock(SettingsModel.AdminRole) : adminRole.Mention) }\n" +
                $"Bot Notifications Role: { (botNotifications == null ? Markdown.InlineCodeBlock(SettingsModel.BotDevRole) : botNotifications.Mention) }\n" +
                $"Minecraft Branch Role: {(minecraftBranch == null ? Markdown.InlineCodeBlock(SettingsModel.MinecraftBranchRole) : minecraftBranch.Mention)}\n" +
                $"Mute Role: {(mute == null ? Markdown.InlineCodeBlock(SettingsModel.MuteRole) : mute.Mention)}");

            string bAdmins = SettingsModel.botAdminIDs.OperationJoin(", ", id =>
            {
                SocketGuildUser user = null;
                if (context.IsGuildContext)
                {
                    user = guildContext.Guild.GetUser(id);
                } 

                if (user != null)
                {
                    return $"{user.Mention} (`{user.Id}`)";
                }
                else
                {
                    return Markdown.InlineCodeBlock(user.Id.ToString());
                }
            });
            embed.AddField($"Bot Admins - {SettingsModel.botAdminIDs.Count}", bAdmins);
            await context.Channel.SendEmbedAsync(embed);
        }
    }

    #endregion
    #region role

    class SetRoleCommand : Command
    {
        public const string SUMMARY = "Gets or sets roles for AccessLevel determination or notifications";
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.1l3vldkfbdlh";
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("RoleIdentifier", $"String identifier for the role you want to get or set. Available are: `{string.Join(", ", Enum.GetNames(typeof(SettingRoles)))}`"),
            new Argument("Role", ArgumentParsing.GENERIC_PARSED_ROLE, true)
        };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };
        public static readonly string REMARKS = $"If the argument {ARGS[1]} is not provided, the current setting is returned instead of setting a new one";

        private SettingRoles RoleIdentifier;
        private SocketRole Role;

        public SetRoleCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (!Enum.TryParse(context.Arguments[0], out RoleIdentifier))
            {
                return new ArgumentParseResult(ARGS[0], $"Could not parse to a role identifier. Available are: `{string.Join(", ", Enum.GetNames(typeof(SettingRoles)))}`");
            }

            if (context.Arguments.Count == 2)
            {
                if (!ArgumentParsing.TryParseRole(context, context.Arguments[1], out Role))
                {
                    return new ArgumentParseResult(ARGS[1], $"Could not parse to a role in this guild");
                }
            }
            else
            {
                Role = null;
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            if (Role == null)
            {
                ulong roleId = 0;
                switch (RoleIdentifier)
                {
                    case SettingRoles.admin:
                        roleId = SettingsModel.AdminRole;
                        break;
                    case SettingRoles.botnotifications:
                        roleId = SettingsModel.BotDevRole;
                        break;
                    case SettingRoles.minecraftbranch:
                        roleId = SettingsModel.MinecraftBranchRole;
                        break;
                    case SettingRoles.mute:
                        roleId = SettingsModel.MuteRole;
                        break;
                    case SettingRoles.guildcaptain:
                        roleId = SettingsModel.GuildCaptainRole;
                        break;
                }

                SocketRole role = context.Guild.GetRole(roleId);

                await context.Channel.SendEmbedAsync($"Current setting for `{RoleIdentifier}` is {(role == null ? Markdown.InlineCodeBlock(roleId) : role.Mention)}");
            }
            else
            {
                switch (RoleIdentifier)
                {
                    case SettingRoles.admin:
                        SettingsModel.AdminRole = Role.Id;
                        break;
                    case SettingRoles.botnotifications:
                        SettingsModel.BotDevRole = Role.Id;
                        break;
                    case SettingRoles.minecraftbranch:
                        SettingsModel.MinecraftBranchRole = Role.Id;
                        break;
                    case SettingRoles.mute:
                        SettingsModel.MuteRole = Role.Id;
                        break;
                    case SettingRoles.guildcaptain:
                        SettingsModel.GuildCaptainRole = Role.Id;
                        break;
                }
                await SettingsModel.SaveSettings();

                await context.Channel.SendEmbedAsync($"Set setting for `{RoleIdentifier}` to {Role.Mention}");
            }
        }

        public enum SettingRoles
        {
            admin,
            botnotifications,
            minecraftbranch,
            mute,
            guildcaptain
        }
    }

    #endregion
    #region channel commands

    #region output

    class SetOutputChannelCommand : Command
    {
        public const string SUMMARY = "Gets or sets output channels";
        public const string REMARKS = "If no channel is provided will return current setting instead";
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.85wziman3nql";
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("OutputChannelType", $"The type of output channel you want to set. Available are: `{Macros.GetEnumNames<OutputChannelType>()}`"),
            new Argument("Channel", ArgumentParsing.GENERIC_PARSED_CHANNEL, true)
        };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public SetOutputChannelCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        private OutputChannelType channelType;
        private SocketGuildChannel channel;

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            channel = null;

            if (!Enum.TryParse(context.Arguments[0], out channelType))
            {
                return new ArgumentParseResult(ARGS[0], $"Could not parse to an output channel type. Available are: `{Macros.GetEnumNames<OutputChannelType>()}`");
            }

            if (context.Arguments.Count == 2)
            {
                if (!ArgumentParsing.TryParseGuildChannel(context, context.Arguments[1], out channel))
                {
                    return new ArgumentParseResult(ARGS[1], $"Could not parse to a channel in this guild");
                }
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            if (channel == null)
            {
                ulong channelId = 0;
                switch (channelType)
                {
                    case OutputChannelType.debuglogging:
                        channelId = GuildChannelHelper.DebugChannelId;
                        break;
                    case OutputChannelType.welcoming:
                        channelId = GuildChannelHelper.WelcomingChannelId;
                        break;
                    case OutputChannelType.admincommandlog:
                        channelId = GuildChannelHelper.AdminCommandUsageLogChannelId;
                        break;
                    case OutputChannelType.adminnotifications:
                        channelId = GuildChannelHelper.AdminNotificationChannelId;
                        break;
                    case OutputChannelType.interactive:
                        channelId = GuildChannelHelper.InteractiveMessagesChannelId;
                        break;
                    case OutputChannelType.guildcategory:
                        channelId = GuildChannelHelper.GuildCategoryId;
                        break;
                }

                channel = context.Guild.GetTextChannel(channelId);
                SocketTextChannel textChannel = channel as SocketTextChannel;

                await context.Channel.SendEmbedAsync($"Current setting for `{channelType}` is {(channel == null ? Markdown.InlineCodeBlock(channelId) : (textChannel == null ? channel.Name : textChannel.Mention))}");
            }
            else
            {
                switch (channelType)
                {
                    case OutputChannelType.debuglogging:
                        GuildChannelHelper.DebugChannelId = channel.Id;
                        break;
                    case OutputChannelType.welcoming:
                        GuildChannelHelper.WelcomingChannelId = channel.Id;
                        break;
                    case OutputChannelType.admincommandlog:
                        GuildChannelHelper.AdminCommandUsageLogChannelId = channel.Id;
                        break;
                    case OutputChannelType.adminnotifications:
                        GuildChannelHelper.AdminNotificationChannelId = channel.Id;
                        break;
                    case OutputChannelType.interactive:
                        GuildChannelHelper.InteractiveMessagesChannelId = channel.Id;
                        break;
                    case OutputChannelType.guildcategory:
                        GuildChannelHelper.GuildCategoryId = channel.Id;
                        break;
                }
                await SettingsModel.SaveSettings();

                SocketTextChannel textChannel = channel as SocketTextChannel;

                await context.Channel.SendEmbedAsync($"Set setting for `{channelType}` to {(textChannel == null ? channel.Name : textChannel.Mention)}");
            }
        }


        private enum OutputChannelType
        {
            debuglogging,
            welcoming,
            admincommandlog,
            adminnotifications,
            interactive,
            guildcategory
        }
    }

    #endregion
    #region channelinfo

    class EditChannelInfoCommand : Command
    {
        public const string SUMMARY = "Retrieve or update channel specific settings";
        public const string REMARKS = "If no Configuration arguments are provided the current setting is printed out";
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.9qit39gxe2bs";
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("Channel", ArgumentParsing.GENERIC_PARSED_CHANNEL),
            new Argument("Configuration",
                $"Configuration arguments, formatted as `<ConfigIdentifier>:<boolean value>`. Available are:\n" +
                $"`{ConfigIdentifier.allowcommands}` - Wether bot commands cna be used in this channel\n" +
                $"`{ConfigIdentifier.allowshitposting}` - Wether shitposting commands are usable in this channel\n\n" +
                $"Example Arguments: `#somechannel {ConfigIdentifier.allowcommands}:true {ConfigIdentifier.allowshitposting}:false` enables commands but disables shitposting in this channel.",
                true, true)
        };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public EditChannelInfoCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        private SocketGuildChannel Channel;
        private List<Tuple<ConfigIdentifier, bool>> Configs = new List<Tuple<ConfigIdentifier, bool>>();

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (!ArgumentParsing.TryParseGuildChannel(context, context.Arguments[0], out Channel))
            {
                return new ArgumentParseResult(ARGS[0], "Failed to parse to a guild channel!");
            }

            Configs.Clear();

            if (context.Arguments.Count > 1)
            {
                context.Arguments.Index++;
                foreach (string arg in context.Arguments)
                {
                    string[] argSplit = arg.Split(':');
                    if (argSplit.Length == 2)
                    {
                        if (!Enum.TryParse(argSplit[0], out ConfigIdentifier configIdentifier))
                        {
                            return new ArgumentParseResult(ARGS[1], $"{arg} - Could not parse to config identifier. Available are `{Macros.GetEnumNames<ConfigIdentifier>()}`!");
                        }
                        if (!bool.TryParse(argSplit[1], out bool setting))
                        {
                            return new ArgumentParseResult(ARGS[1], $"{arg} - Could not parse boolean value!");
                        }
                        Configs.Add(new Tuple<ConfigIdentifier, bool>(configIdentifier, setting));
                    }
                    else
                    {
                        return new ArgumentParseResult(ARGS[1], $"{arg} - Could not split into config identifer and setting!");
                    }
                }
                context.Arguments.Index--;
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            bool foundExisting = GuildChannelHelper.TryGetChannelConfig(Channel.Id, out GuildChannelConfiguration channelConfig);

            if (!foundExisting)
            {
                channelConfig = new GuildChannelConfiguration(Channel.Id);
            }

            foreach (var config in Configs)
            {
                switch (config.Item1)
                {
                    case ConfigIdentifier.allowshitposting:
                        channelConfig.AllowShitposting = config.Item2;
                        break;
                    case ConfigIdentifier.allowcommands:
                        channelConfig.AllowCommands = config.Item2;
                        break;
                }
            }

            GuildChannelHelper.SetChannelConfig(channelConfig);

            await SettingsModel.SaveSettings();

            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = BotCore.EmbedColor,
                Description = channelConfig.ToString()
            };
            if (Configs.Count > 0)
            {
                embed.Title = $"Configuration applied for channel {Channel.Name}";
            }
            else
            {
                embed.Title = $"Configuration of channel {Channel.Name}";
            }

            await context.Channel.SendEmbedAsync(embed);
        }

        enum ConfigIdentifier
        {
            allowshitposting,
            allowcommands
        }
    }

    #endregion

    #endregion
    #region template

    class SetTemplateCommand : Command
    {
        public const string SUMMARY = "Gets or sets message templates";
        public const string REMARKS = "If the argument `([Text])` is not provided, the current template is returned instead of setting a new one";
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.j9fbgsio3olf";
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("TemplateIdentifier", $"String identifier for the template you want to get or set. Available are: `{string.Join(", ", Enum.GetNames(typeof(Templates)))}`"),
            new Argument("Text", "These arguments combined represent the new text for the template", true, true)
        };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        private Templates template;
        private string newText;

        public SetTemplateCommand(string identifier) : base(identifier, OverriddenMethod.BasicSynchronous, OverriddenMethod.BasicAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            if (!Enum.TryParse(context.Arguments.First, out template))
            {
                return new ArgumentParseResult(ARGS[0], $"Could not parse to a template identifier. Available are: `{string.Join(", ", Enum.GetNames(typeof(Templates)))}`");
            }

            if (context.Arguments.Count > 1 && context.Message.Content.Length > Identifier.Length + context.Arguments.First.Length + 2)
            {
                newText = context.Message.Content.Substring(Identifier.Length + context.Arguments.First.Length + 2);
            }
            else
            {
                newText = null;
            }
            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            if (newText == null)
            {
                switch (template)
                {
                    case Templates.welcominginfo:
                        if (SettingsModel.welcomingMessage.Contains("{0}"))
                        {
                            await context.Channel.SendEmbedAsync($"Welcome {context.User.Mention}!", string.Format(SettingsModel.welcomingMessage, context.User.Mention));
                        }
                        else
                        {
                            await context.Channel.SendEmbedAsync($"Welcome {context.User.Mention}!", SettingsModel.welcomingMessage);
                        }
                        break;
                }
            }
            else
            {
                switch (template)
                {
                    case Templates.welcominginfo:
                        SettingsModel.welcomingMessage = newText;
                        await SettingsModel.SaveSettings();
                        if (SettingsModel.welcomingMessage.Contains("{0}"))
                        {
                            await context.Channel.SendEmbedAsync($"Welcome {context.User.Mention}!", string.Format(SettingsModel.welcomingMessage, context.User.Mention));
                        }
                        else
                        {
                            await context.Channel.SendEmbedAsync($"Welcome {context.User.Mention}!", SettingsModel.welcomingMessage);
                        }
                        break;
                }
            }
        }

        enum Templates
        {
            welcominginfo
        }
    }

    #endregion
    #region prefix

    class PrefixCommand : Command
    {
        public const string SUMMARY = "Sets the command prefix the bot should use";
        public const string REMARKS = default;
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.kw660g9sdfi5";
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("PrefixCharacter", "A character to set as the new command prefix")
        };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        char Prefix;

        public PrefixCommand(string identifier) : base(identifier, OverriddenMethod.BasicSynchronous, OverriddenMethod.BasicAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            Prefix = context.Arguments.First[0];
            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            CommandHandler.Prefix = Prefix;
            await SettingsModel.SaveSettings();
            await context.Channel.SendEmbedAsync($"Set the command prefix to `{Prefix}`");
        }
    }

    #endregion
    #region logging

    class ToggleLoggingCommand : Command
    {
        public const string SUMMARY = "Toggles logging for specific message categories";
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.l2sb9l9seq78";
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("MessageCategory", $"Specifies the logging message category that you want to get or set. Available are: `{Macros.GetEnumNames<DebugCategories>()}`"),
            new Argument("Enabled", "Specify the new setting for the given MessageType. Use `true`, `false`, `enable` or `disable`", true)
        };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };
        public static readonly string REMARKS = $"If `{ARGS[1]}` is not provided, will display the current setting instead";

        private DebugCategories category;
        private bool? newSetting;

        public ToggleLoggingCommand(string identifier) : base(identifier, OverriddenMethod.BasicSynchronous, OverriddenMethod.BasicAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            if (!Enum.TryParse(context.Arguments.First, out category))
            {
                return new ArgumentParseResult(ARGS[0], $"Failed to parse to a logging category! Available are: `{Macros.GetEnumNames<DebugCategories>()}`");
            }

            if (context.Arguments.Count > 1)
            {
                string setting = context.Arguments[1];
                if (setting.Contains("enable"))
                {
                    newSetting = true;
                }
                else if (setting.Contains("disable"))
                {
                    newSetting = false;
                }
                else if (bool.TryParse(setting, out bool setting_b))
                {
                    newSetting = setting_b;
                }
                else
                {
                    return new ArgumentParseResult(ARGS[1], "Could not parse to a boolean value! Use `true`, `false`, `enable` or `disable`");
                }
            }
            else
            {
                newSetting = null;
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            if (newSetting == null)
            {
                bool value = SettingsModel.debugLogging[(int)category];
                await context.Channel.SendEmbedAsync($"Loggging for messages of category `{category}` is {(value ? "enabled" : "disabled")}");
            }
            else
            {
                SettingsModel.debugLogging[(int)category] = (bool)newSetting;
                await SettingsModel.SaveSettings();
                await context.Channel.SendEmbedAsync($"{((bool)newSetting ? "Enabled" : "Disabled")} loggging for messages of category `{category}`");
            }
        }
    }

    #endregion

    // Stop and Restart

    #region stop

    class StopCommand : Command
    {
        public const string SUMMARY = "Stops the bot application. Requires manual restart";
        public const string REMARKS = default;
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] { };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public StopCommand(string identifier) : base(identifier, OverriddenMethod.None, OverriddenMethod.BasicSynchronous, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        protected override void HandleCommandSynchronous(CommandContext context)
        {
            Var.running = false;
        }
    }

    #endregion
    #region restart

    class RestartCommand : Command
    {
        public const string SUMMARY = "Restarts the bot application";
        public const string REMARKS = default;
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.d373hflw2kml";
        public static readonly Argument[] ARGS = new Argument[] { };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public RestartCommand(string identifier) : base(identifier, OverriddenMethod.None, OverriddenMethod.BasicSynchronous, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        protected override void HandleCommandSynchronous(CommandContext context)
        {
            Var.RestartPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            Var.running = false;
        }
    }

    #endregion
    #region AutoRoles

    class AutoRoleCommand : Command
    {
        public const string SUMMARY = "List, add or remove roles that are automatically added to any user that joins";
        public const string REMARKS = "If executed without arguments, lists all autoroles instead!";
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("Mode", $"What mode the command runs in. Available are: `{Macros.GetEnumNames<CommandMode>()}`", optional:true),
            new Argument("Role", ArgumentParsing.GENERIC_PARSED_ROLE, optional:true)
        };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public AutoRoleCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        private enum CommandMode
        {
            list,
            add,
            remove
        }

        private CommandMode Mode;
        private SocketRole Role;
        private ulong RoleId;

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (context.Arguments.Count == 0)
            {
                Mode = CommandMode.list;
                Role = null;
            }
            else
            {
                if (!Enum.TryParse(context.Arguments.First, out Mode))
                {
                    return new ArgumentParseResult(ARGS[0], $"Unable to parse `{context.Arguments.First}` to a valid command mode!");
                }

                context.Arguments.Index++;

                if (context.Arguments.Count == 0 && Mode != CommandMode.list)
                {
                    return new ArgumentParseResult(ARGS[1], $"Mode `{Mode}` requires a role as second argument!");
                }

                if (Mode != CommandMode.list)
                {
                    if (!ArgumentParsing.TryParseRole(context, context.Arguments.First, out Role))
                    {
                        if (Mode == CommandMode.add)
                        {
                            return new ArgumentParseResult(ARGS[1], $"Could not parse `{context.Arguments.First}` to a valid role!");
                        }
                        else
                        {
                            if (!ulong.TryParse(context.Arguments.First, out RoleId))
                            {
                                return new ArgumentParseResult(ARGS[1], $"Could not parse `{context.Arguments.First}` to a valid role!");
                            }
                        }
                    }

                    if (Role != null)
                    {
                        RoleId = Role.Id;
                    }

                    bool hasRole = EventLogger.AutoAssignRoleIds.Contains(RoleId);
                    if (Mode == CommandMode.add && hasRole)
                    {
                        return new ArgumentParseResult(ARGS[1], $"{Role.Mention} is already amongst the auto assign roles!");
                    }
                    else if (Mode == CommandMode.remove && !hasRole)
                    {
                        return new ArgumentParseResult(ARGS[1], $"Can not remove {Role.Mention} from the list of auto assign roles, as it isn't in that list already!");
                    }
                }
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            switch (Mode)
            {
                case CommandMode.list:
                    EmbedBuilder embed = new EmbedBuilder()
                    {
                        Title = "Roles automatically assigned to newly joined users",
                        Color = BotCore.EmbedColor,
                        Description = EventLogger.AutoAssignRoleIds.Count == 0 ?
                        "No Roles"
                        :
                        EventLogger.AutoAssignRoleIds.OperationJoin(", ", (ulong roleId) =>
                        {
                            SocketRole role = context.Guild.GetRole(roleId);
                            if (role != null)
                            {
                                return role.Mention;
                            }
                            else
                            {
                                return Markdown.InlineCodeBlock(roleId);
                            }
                        })
                    };
                    await context.Channel.SendEmbedAsync(embed);
                    break;
                case CommandMode.add:
                    EventLogger.AutoAssignRoleIds.Add(RoleId);
                    await SettingsModel.SaveSettings();
                    await context.Channel.SendEmbedAsync($"Added {Role.Mention} to the list of roles automatically assigned to new users!");
                    break;
                case CommandMode.remove:
                    EventLogger.AutoAssignRoleIds.Remove(RoleId);
                    await SettingsModel.SaveSettings();
                    await context.Channel.SendEmbedAsync($"Removed {(Role == null ? Markdown.InlineCodeBlock(RoleId) : Role.Mention)} from the list of roles automatically assigned to new users!");
                    break;
            }
        }
    }

    #endregion
    #region value

    class ValueCommand : Command
    {
        public const string SUMMARY = "List, check and set ConfigValues";
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("ValueIdentifier", $"What ConfigValue to access. Available are: `{Macros.GetEnumNames<Values>()}`, see remarks for details.", optional:true),
            new Argument("Value", $"Supply this if you want to override the existing value", optional:true)
        };
        public static readonly string REMARKS = $"Lists all values if neither {ARGS[0]} and {ARGS[1]} are supplied. Details on Values:" +
            $"`{Values.WelcomingMode}` - Where welcoming info is sent to. Set to `0` for Welcoming Channel, and `1` for PMs";
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        private enum Values
        {
            WelcomingMode
        }

        public ValueCommand(string identifier) : base(identifier, OverriddenMethod.BasicSynchronous, OverriddenMethod.BasicAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK) { }
    }

    #endregion
}
#endif