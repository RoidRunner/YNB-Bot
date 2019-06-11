using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.NestedCommands
{
    // Config

    #region detect

    class DetectConfigCommand : Command
    {
        public override string Identifier => "detect";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicSynchronous;

        public DetectConfigCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;

            InitializeHelp("Lists current configuration", new CommandArgument[0]);
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            return ArgumentParseResult.DefaultNoArguments;
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

            if (GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext))
            {
                adminRole = guildContext.Guild.GetRole(SettingsModel.AdminRole);
                botNotifications = guildContext.Guild.GetRole(SettingsModel.BotDevRole);
                minecraftBranch = guildContext.Guild.GetRole(SettingsModel.MinecraftBranchRole);
            }

            EmbedBuilder embed = new EmbedBuilder()
            {
                Title = "Current Settings",
                Color = Var.BOTCOLOR,
                Description = $"YNB Bot {Var.VERSION}"
            };
            StringBuilder debugLogging = new StringBuilder("Logging Channel: ");
            if (debugChannel == null)
            {
                debugLogging.AppendLine(Macros.InlineCodeBlock(GuildChannelHelper.DebugChannelId));
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
            embed.AddField("Channels", $"Welcoming: { (welcomingChannel == null ? Macros.InlineCodeBlock(GuildChannelHelper.WelcomingChannelId) : welcomingChannel.Mention) }\n" +
                $"Interactive Messages: {(interactiveMessagesChannel == null ? Macros.InlineCodeBlock(GuildChannelHelper.InteractiveMessagesChannelId) : interactiveMessagesChannel.Mention)}\n" +
                $"Admin Command Usage Logging: {(adminCommandUsageLogging == null ? Macros.InlineCodeBlock(GuildChannelHelper.AdminCommandUsageLogChannelId) : adminCommandUsageLogging.Mention)}\n" +
                $"Admin Notifications: {(adminNotificationChannel == null ? Macros.InlineCodeBlock(GuildChannelHelper.AdminNotificationChannelId) : adminNotificationChannel.Mention)}");

            embed.AddField("Roles", $"Admin Role: { (adminRole == null ? Macros.InlineCodeBlock(SettingsModel.AdminRole) : adminRole.Mention) }\n" +
                $"Bot Notifications Role: { (botNotifications == null ? Macros.InlineCodeBlock(SettingsModel.BotDevRole) : botNotifications.Mention) }\n" +
                $"Minecraft Branch Role: {(minecraftBranch == null ? Macros.InlineCodeBlock(SettingsModel.MinecraftBranchRole) : minecraftBranch.Mention)}");
            await context.Channel.SendEmbedAsync(embed);
        }
    }

    #endregion
    #region role

    class SetRoleCommand : Command
    {
        public override string Identifier => "role";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.GuildAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.GuildSynchronous;

        private SettingRoles RoleIdentifier;
        private SocketRole Role;

        public SetRoleCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;

            List<CommandArgument> arguments = new List<CommandArgument>();
            arguments.Add(new CommandArgument("RoleIdentifier", $"String identifier for the role you want to get or set. Available are: `{string.Join(", ", Enum.GetNames(typeof(SettingRoles)))}`"));
            arguments.Add(new CommandArgument("Role", ArgumentParsingHelper.GENERIC_PARSED_ROLE, true));
            InitializeHelp("Gets or sets roles for AccessLevel determination or notifications",
                arguments.ToArray(), remarks: "If the argument `(Role)` is not provided, the current setting is returned instead of setting a new one");
        }

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (!Enum.TryParse(context.Args[0], out RoleIdentifier))
            {
                return new ArgumentParseResult(Arguments[0], $"Could not parse to a role identifier. Available are: `{string.Join(", ", Enum.GetNames(typeof(SettingRoles)))}`");
            }

            if (context.Args.Count == 2)
            {
                if (!ArgumentParsingHelper.TryParseRole(context, context.Args[1], out Role))
                {
                    return new ArgumentParseResult(Arguments[1], $"Could not parse to a role in this guild");
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
                }

                SocketRole role = context.Guild.GetRole(roleId);

                await context.Channel.SendEmbedAsync($"Current setting for `{RoleIdentifier}` is {(role == null ? Macros.InlineCodeBlock(roleId) : role.Mention)}");
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
                }
                await SettingsModel.SaveSettings();

                await context.Channel.SendEmbedAsync($"Set setting for `{RoleIdentifier}` to {Role.Mention}");
            }
        }

        public enum SettingRoles
        {
            admin,
            botnotifications,
            minecraftbranch
        }
    }

    #endregion
    #region channel commands

    #region output

    class SetOutputChannelCommand : Command
    {
        public override string Identifier => "output";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.GuildAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.GuildSynchronous;

        public SetOutputChannelCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;
            CommandArgument[] arguments = new CommandArgument[2];
            arguments[0] = new CommandArgument("OutputChannelType", $"The type of output channel you want to set. Available are: `{Macros.GetEnumNames<OutputChannelType>()}`");
            arguments[1] = new CommandArgument("Channel", ArgumentParsingHelper.GENERIC_PARSED_CHANNEL, true);
            InitializeHelp("Gets or sets output channels", arguments, "If no channel is provided will return current setting instead");
        }

        private OutputChannelType channelType;
        private SocketGuildChannel channel;

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            channel = null;

            if (!Enum.TryParse(context.Args[0], out channelType))
            {
                return new ArgumentParseResult(Arguments[0], $"Could not parse to an output channel type. Available are: `{Macros.GetEnumNames<OutputChannelType>()}`");
            }

            if (context.Args.Count == 2)
            {
                if (!ArgumentParsingHelper.TryParseGuildChannel(context, context.Args[1], out channel))
                {
                    return new ArgumentParseResult(Arguments[1], $"Could not parse to a channel in this guild");
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

                await context.Channel.SendEmbedAsync($"Current setting for `{channelType}` is {(channel == null ? Macros.InlineCodeBlock(channelId) : (textChannel == null ? channel.Name : textChannel.Mention))}");
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
        public override string Identifier => "channel";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.GuildAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.GuildSynchronous;

        public EditChannelInfoCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;

            CommandArgument[] arguments = new CommandArgument[2];
            arguments[0] = new CommandArgument("Channel", ArgumentParsingHelper.GENERIC_PARSED_CHANNEL);
            arguments[1] = new CommandArgument("Configuration",
                $"Configuration arguments, formatted as `<ConfigIdentifier>:<boolean value>`. Available are:\n" +
                $"`{ConfigIdentifier.allowcommands}` - Wether bot commands cna be used in this channel\n" +
                $"`{ConfigIdentifier.allowshitposting}` - Wether shitposting commands are usable in this channel\n\n" +
                $"Example Arguments: `#somechannel {ConfigIdentifier.allowcommands}:true {ConfigIdentifier.allowshitposting}:false` enables commands but disables shitposting in this channel.",
                true, true);
            InitializeHelp("Retrieve or update channel specific settings", arguments, "If no Configuration arguments are provided the current setting is printed out.");
        }

        private SocketGuildChannel Channel;
        private List<Tuple<ConfigIdentifier, bool>> Configs = new List<Tuple<ConfigIdentifier, bool>>();

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (!ArgumentParsingHelper.TryParseGuildChannel(context, context.Args[0], out Channel))
            {
                return new ArgumentParseResult(Arguments[0], "Failed to parse to a guild channel!");
            }

            Configs.Clear();

            if (context.Args.Count > 1)
            {
                context.Args.Index++;
                foreach (string arg in context.Args)
                {
                    string[] argSplit = arg.Split(':');
                    if (argSplit.Length == 2)
                    {
                        if (!Enum.TryParse(argSplit[0], out ConfigIdentifier configIdentifier))
                        {
                            return new ArgumentParseResult(Arguments[1], $"{arg} - Could not parse to config identifier. Available are `{Macros.GetEnumNames<ConfigIdentifier>()}`!");
                        }
                        if (!bool.TryParse(argSplit[1], out bool setting))
                        {
                            return new ArgumentParseResult(Arguments[1], $"{arg} - Could not parse boolean value!");
                        }
                        Configs.Add(new Tuple<ConfigIdentifier, bool>(configIdentifier, setting));
                    }
                    else
                    {
                        return new ArgumentParseResult(Arguments[1], $"{arg} - Could not split into config identifer and setting!");
                    }
                }
                context.Args.Index--;
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
                Color = Var.BOTCOLOR,
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
        public override string Identifier => "template";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicSynchronous;

        private Templates template;
        private string newText;

        public SetTemplateCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;

            List<CommandArgument> arguments = new List<CommandArgument>(2);
            arguments.Add(new CommandArgument("TemplateIdentifier", $"String identifier for the template you want to get or set. Available are: `{string.Join(", ", Enum.GetNames(typeof(Templates)))}`"));
            arguments.Add(new CommandArgument("Text", "These arguments combined represent the new text for the template", true, true));
            InitializeHelp("Gets or sets message templates for welcoming, etc.", arguments.ToArray(), "If the argument `([Text])` is not provided, the current template is returned instead of setting a new one");
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            if (!Enum.TryParse(context.Args.First, out template))
            {
                return new ArgumentParseResult(Arguments[0], $"Could not parse to a template identifier. Available are: `{string.Join(", ", Enum.GetNames(typeof(Templates)))}`");
            }

            if (context.Args.Count > 1 && context.Message.Content.Length > FullIdentifier.Length + context.Args.First.Length + 2)
            {
                newText = context.Message.Content.Substring(FullIdentifier.Length + context.Args.First.Length + 2);
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
        public override string Identifier => "prefix";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicSynchronous;

        char Prefix;

        public PrefixCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;

            List<CommandArgument> arguments = new List<CommandArgument>();
            arguments.Add(new CommandArgument("PrefixCharacter", "A character to set as the new command prefix"));
            InitializeHelp("Sets the command prefix the bot should use", arguments.ToArray());
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            Prefix = context.Args.First[0];
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
        public override string Identifier => "logging";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicSynchronous;

        private DebugCategories category;
        private bool? newSetting;

        public ToggleLoggingCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;

            List<CommandArgument> arguments = new List<CommandArgument>(2);
            arguments.Add(new CommandArgument("MessageCategory", $"Specifies the logging message category that you want to get or set. Available are: `{Macros.GetEnumNames<DebugCategories>()}`"));
            arguments.Add(new CommandArgument("Enabled", "Specify the new setting for the given MessageType. Use `true`, `false`, `enable` or `disable`", true));
            InitializeHelp("Toggles logging for specific message categories", arguments.ToArray(), $"If the argument `{arguments[1]}` is not provided, will display the current setting instead");
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            if (!Enum.TryParse(context.Args.First, out category))
            {
                return new ArgumentParseResult(Arguments[0], $"Failed to parse to a logging category! Available are: `{Macros.GetEnumNames<DebugCategories>()}`");
            }

            if (context.Args.Count > 1)
            {
                string setting = context.Args[1];
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
                    return new ArgumentParseResult(Arguments[1], "Could not parse to a boolean value! Use `true`, `false`, `enable` or `disable`");
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
        public override string Identifier => "stop";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicSynchronous;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicSynchronous;

        public StopCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;

            InitializeHelp("Stops the bot application. Requires manual restart", new CommandArgument[0]);
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            return ArgumentParseResult.DefaultNoArguments;
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
        public override string Identifier => "restart";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicSynchronous;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicSynchronous;

        public RestartCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;

            InitializeHelp("Restarts the bot application", new CommandArgument[0]);
        }

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            return ArgumentParseResult.DefaultNoArguments;
        }

        protected override void HandleCommandSynchronous(CommandContext context)
        {
            Var.RestartPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            Var.running = false;
        }
    }

    #endregion
}
