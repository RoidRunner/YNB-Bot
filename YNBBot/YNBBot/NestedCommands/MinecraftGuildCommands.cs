﻿using BotCoreNET;
using BotCoreNET.CommandHandling;
using BotCoreNET.Helpers;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YNBBot.Interactive;
using YNBBot.MinecraftGuildSystem;

namespace YNBBot.NestedCommands
{

    #region found
    class CreateGuildCommand : Command
    {
        public const string SUMMARY = "Requests creation of a new minecraft guild";
        public const string LINK = "https://docs.google.com/document/d/1IdTQoq2l9YhF5Tlj5lBYz5Zcz56NQEgL3Hg5Dg2RyWs/edit#heading=h.h41js19sf4v4";
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("Name", "The name of the guild. Will be the name of the channel and role created. Also applies to ingame naming"),
            new Argument("Color", $"The color of the guild. Will be the color of the role created. Also applies to ingame color. Available are `{string.Join(", ", MinecraftGuildSystem.MinecraftGuildModel.AvailableColors)}`"),
            new Argument("Members", "Minimum of two members, selected either by discord snowflake id, mention or Username#Discriminator", multiple: true)};
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.MINECRAFT };
        public static readonly string REMARKS = $"{ARGS[0]} and {ARGS[1]} have to be free to take, all invited members ({ARGS[2]}) have to accept the invitation, and an admin has to confirm the creation of the new guild";

        public CreateGuildCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.BasicAsync, arguments: ARGS, preconditions: AUTHCHECKS, summary: SUMMARY, remarks: REMARKS, helplink: LINK)
        {
        }

        string GuildName;
        GuildColor GuildColor;
        List<SocketGuildUser> Members = new List<SocketGuildUser>();

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (MinecraftGuildSystem.MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out MinecraftGuild contextUserGuild))
            {
                if (contextUserGuild.Active)
                {
                    return new ArgumentParseResult($"You can not found a new guild because you are still part of `{contextUserGuild.Name}`");
                }
                else if (contextUserGuild.CaptainId == context.User.Id)
                {
                    return new ArgumentParseResult($"You can not found a new guild because you captain of the inactivated guild `{contextUserGuild.Name}`. Please contact an admin!");
                }
            }

            GuildName = context.Arguments[0];

            context.Arguments.Index++;

            if (GuildName.StartsWith('\"'))
            {
                for (; context.Arguments.Index < context.Arguments.TotalCount; context.Arguments.Index++)
                {
                    GuildName += " " + context.Arguments.First;
                    if (context.Arguments.First.EndsWith('\"'))
                    {
                        context.Arguments.Index++;
                        break;
                    }
                }

                GuildName = GuildName.Trim('\"');
            }

            if (GuildName.Length < 3)
            {
                return new ArgumentParseResult(ARGS[0], "Too short! Minimum of 3 Characters");
            }
            if (GuildName.Length > 50)
            {
                return new ArgumentParseResult(ARGS[0], "Too long! Maximum of 50 Characters");
            }
            if (!MinecraftGuildModel.NameIsLegal(GuildName))
            {
                return new ArgumentParseResult(ARGS[0], "The guild name contains illegal characters! (Or starts/ends with a whitespace)");
            }
            if (!MinecraftGuildModel.NameIsAvailable(GuildName))
            {
                return new ArgumentParseResult(ARGS[0], "A guild with this name already exists!");
            }

            if (!Enum.TryParse(context.Arguments[0], out GuildColor))
            {
                return new ArgumentParseResult(ARGS[1], $"Could not parse to an available guild color! Available are `{string.Join(", ", MinecraftGuildModel.AvailableColors)}`");
            }
            if (!MinecraftGuildModel.ColorIsAvailable(GuildColor))
            {
                return new ArgumentParseResult(ARGS[1], $"A guild with this color already exists! Available are `{string.Join(", ", MinecraftGuildModel.AvailableColors)}`");
            }

            context.Arguments.Index++;

            Members.Clear();
            if (context.Arguments.Count < MinecraftGuildModel.MIN_GUILDFOUNDINGMEMBERS)
            {
                return new ArgumentParseResult(ARGS[2], $"You need to supply a minimum of {MinecraftGuildModel.MIN_GUILDFOUNDINGMEMBERS} members!");
            }

            for (int i = 0; i < context.Arguments.Count; i++)
            {
                if (ArgumentParsing.TryParseGuildUser(context, context.Arguments[i], out SocketGuildUser member, allowSelf: false))
                {
                    if (member.Id == context.User.Id)
                    {
                        return new ArgumentParseResult(ARGS[2], "Can not add yourself as a guild member!");
                    }
                    if (MinecraftGuildSystem.MinecraftGuildModel.TryGetGuildOfUser(member.Id, out MinecraftGuild memberGuild))
                    {
                        if (memberGuild.Active)
                        {
                            return new ArgumentParseResult(ARGS[2], $"Can not invite {member.Mention}, because he is already part of \"{memberGuild.Name}\"");
                        }
                        else if (memberGuild.CaptainId == member.Id)
                        {
                            return new ArgumentParseResult(ARGS[2], $"Can not invite {member.Mention}, because he is captain of inactivated guild \"{memberGuild.Name}\". Please contact an admin!");
                        }
                    }
                    bool hasMinecraftRole = false;
                    foreach (SocketRole role in member.Roles)
                    {
                        if (role.Id == SettingsModel.MinecraftBranchRole)
                        {
                            hasMinecraftRole = true;
                            break;
                        }
                    }

                    if (!hasMinecraftRole)
                    {
                        return new ArgumentParseResult(ARGS[2], $"Can not invite {member.Mention}, because he is not part of the minecraft branch!");
                    }

                    Members.Add(member);
                }
                else
                {
                    return new ArgumentParseResult(ARGS[2], $"Could not parse `{context.Arguments[i]}` to a guild user!");
                }
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            MinecraftGuild guild = new MinecraftGuild()
            {
                Name = GuildName,
                Color = GuildColor,
                CaptainId = context.User.Id,
                MemberIds = new List<ulong>()
            };

            await GuildCreationInteractiveMessage.FromNewGuildAndMemberList(guild, Members);
        }
    }

    #endregion
    #region modify

    class ModifyGuildCommand : Command
    {
        public const string SUMMARY = "Modifies guild attributes";
        public static readonly string REMARKS = $"For a list of modifying actions see below. Some actions require an argument to be passed after them following this syntax: `<Action>:<Argument>`. Multiword arguments are to be encased with quotation marks '\"'.\n\n" +
                $"`{GuildModifyActions.delete}` - Removes the guild dataset, channel and role\n" +
                $"`{GuildModifyActions.deletedataset}` - Removes the guild dataset\n" +
                $"`{GuildModifyActions.setactive}<Active>` - Sets the guild active/inactive (boolean value)\n" +
                $"`{GuildModifyActions.setchannel}:<Channel>` - Sets the guild channel\n" +
                $"`{GuildModifyActions.setrole}:<Role>` - Sets the guild role\n" +
                $"`{GuildModifyActions.rename}:<Name>` - Renames the guild\n" +
                $"`{GuildModifyActions.recolor}:<Color>` - Assignes a new color to the guild\n" +
                $"`{GuildModifyActions.setcaptain}:<Captain>` - Sets the captain of the guild. Has to be a member!\n" +
                $"`{GuildModifyActions.addmember}:<Member>` - Manually adds a member to the guild\n" +
                $"`{GuildModifyActions.removemember}:<Member>` - Manually removes a member from the guild" +
                $"`{GuildModifyActions.timestamp}:<Timestamp>` - Sets the founding timestamp for this guild. Format is a variant of ISO 8601: `YYYY-MM-DD hh:mm:ssZ`, example: `2019-06-11 17:55:35Z`";
        public static readonly Argument[] ARGS = new Argument[] {
                new Argument("Name", "Name of the guild to delete"),
                new Argument("Actions", $"The modifying action you want to take. For a list of modifying actions see remarks", multiple:true)
        };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] 
        {
            AccessLevelAuthPrecondition.ADMIN
        };
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.z9qkxx7wamoo";

        public ModifyGuildCommand(string identifier) : base(identifier, OverriddenMethod.BasicSynchronous, OverriddenMethod.BasicAsync, true, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        private enum GuildModifyActions
        {
            delete,
            deletedataset,
            setactive,
            rename,
            recolor,
            setchannel,
            setrole,
            setcaptain,
            addmember,
            removemember,
            timestamp
        }

        private readonly bool[] ActionRequiresArg = new bool[] { false, false, true, true, true, true, true, true, true, true, true };

        private struct GuildAction : IComparable
        {
            public GuildModifyActions Action;
            public string Argument;

            public GuildAction(GuildModifyActions action, string argument)
            {
                Action = action;
                Argument = argument;
            }

            public int CompareTo(object obj)
            {
                if (obj is GuildAction)
                {
                    return Action.CompareTo(((GuildAction)obj).Action);
                }
                else
                {
                    return 0;
                }
            }

            public override string ToString()
            {
                if (string.IsNullOrEmpty(Argument))
                {
                    return Action.ToString();
                }
                else if (Argument.Contains(' '))
                {
                    return $"{Action}:\"{Argument}\"";
                }
                else
                {
                    return $"{Action}:{Argument}";
                }

            }
        }

        private List<GuildAction> Actions = new List<GuildAction>();
        private MinecraftGuild TargetGuild;

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            if (!ArgumentParsing.TryParseMinecraftGuild(context.Arguments, out string parsedName, out TargetGuild))
            {
                return new ArgumentParseResult(ARGS[0], $"Unable to find a guild named `{parsedName}`");
            }

            Actions.Clear();

            GuildModifyActions action = GuildModifyActions.delete;
            string arg = string.Empty;
            ModifyActionsParseStep parseStep = ModifyActionsParseStep.IdentifyAction;
            for (; context.Arguments.Count > 0; context.Arguments.Index++)
            {
                string current = context.Arguments.First;
                switch (parseStep)
                {
                    case ModifyActionsParseStep.IdentifyAction:
                        if (current.Contains(':'))
                        {
                            string[] splits = current.Split(':', StringSplitOptions.RemoveEmptyEntries);
                            if (splits.Length > 0)
                            {
                                if (!Enum.TryParse(splits[0], out action))
                                {
                                    return new ArgumentParseResult(ARGS[1], $"Unable to parse `{splits[0]}` to a guild modify action!");
                                }
                                arg = splits[1];
                                for (int i = 2; i < splits.Length; i++)
                                {
                                    arg += ':' + splits[i];
                                }
                                if (arg.StartsWith('\"'))
                                {
                                    parseStep = ModifyActionsParseStep.AppendArgument;
                                }
                                else
                                {
                                    Actions.Add(new GuildAction(action, arg));
                                }
                            }
                            else
                            {
                                return new ArgumentParseResult(ARGS[1], $"Unable to parse `{current}` to a guild modify action!");
                            }
                        }
                        else if (Enum.TryParse(current, out action))
                        {
                            if (requireArgument(action))
                            {
                                return new ArgumentParseResult(ARGS[1], $"Action `{action}` requires an argument!");
                            }
                            else
                            {
                                Actions.Add(new GuildAction(action, null));
                            }
                        }
                        else
                        {
                            return new ArgumentParseResult(ARGS[1], $"Unable to parse `{current}` to a guild modify action!");
                        }
                        break;
                    case ModifyActionsParseStep.AppendArgument:
                        arg += ' ' + current;
                        if (arg.EndsWith('\"'))
                        {
                            arg = arg.Substring(1, arg.Length - 2);
                            Actions.Add(new GuildAction(action, arg));
                            parseStep = ModifyActionsParseStep.IdentifyAction;
                        }
                        break;
                }
            }

            if (parseStep == ModifyActionsParseStep.AppendArgument)
            {
                return new ArgumentParseResult(ARGS[1], $"Argument {arg} was not terminated with quotation marks '\"'");
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        private bool requireArgument(GuildModifyActions action)
        {
            return ActionRequiresArg[(int)action];
        }

        private enum ModifyActionsParseStep
        {
            IdentifyAction,
            AppendArgument
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            bool hasGuildContext = GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext);

            List<string> errors = new List<string>();
            List<GuildAction> successful = new List<GuildAction>();

            bool saveChanges = false;
            Actions.Sort();
            for (int i = 0; i < Actions.Count; i++)
            {
                GuildAction action = Actions[i];
                switch (action.Action)
                {
                    case GuildModifyActions.delete:
                        if (hasGuildContext)
                        {
                            if (await MinecraftGuildModel.DeleteGuildAsync(guildContext.Guild, TargetGuild))
                            {
                                successful.Add(action);
                            }
                            else
                            {
                                errors.Add($"`{action}` - Internal Error deleting Guild!");
                            }
                            i = Actions.Count;
                        }
                        else
                        {
                            errors.Add($"`{action}` - Requires Guild Context!");
                        }
                        break;
                    case GuildModifyActions.deletedataset:
                        await MinecraftGuildModel.DeleteGuildDatasetAsync(TargetGuild);
                        i = Actions.Count;
                        break;
                    case GuildModifyActions.setactive:
                        if (hasGuildContext)
                        {
                            switch (action.Argument.ToLower())
                            {
                                case "active":
                                case "true":
                                    if (!TargetGuild.Active)
                                    {
                                        if (await MinecraftGuildModel.SetGuildActive(guildContext.Guild, TargetGuild, true))
                                        {
                                            successful.Add(action);
                                        }
                                        else
                                        {
                                            errors.Add($"`{action}` - Internal error setting guild active!");
                                        }
                                    }
                                    else
                                    {
                                        errors.Add($"`{action}` - Guild already active!");
                                    }
                                    break;
                                case "inactive":
                                case "false":
                                    if (TargetGuild.Active)
                                    {
                                        if (await MinecraftGuildModel.SetGuildActive(guildContext.Guild, TargetGuild, false))
                                        {
                                            successful.Add(action);
                                        }
                                        else
                                        {
                                            errors.Add($"`{action}` - Internal error setting guild inactive!");
                                        }
                                    }
                                    else
                                    {
                                        errors.Add($"`{action}` - Guild already inactive!");
                                    }
                                    break;
                                default:
                                    errors.Add($"`{action}` - Could not parse argument to a valid boolean value!");
                                    break;
                            }
                        }
                        else
                        {
                            errors.Add($"`{action}` - Requires Guild Context!");
                        }
                        i = Actions.Count;
                        break;
                    case GuildModifyActions.setchannel:
                        if (ArgumentParsing.TryParseGuildTextChannel(context, action.Argument, out SocketTextChannel newGuildChannel))
                        {
                            await newGuildChannel.ModifyAsync(GuildChannelProperties =>
                            {
                                GuildChannelProperties.Name = TargetGuild.Name;
                                GuildChannelProperties.CategoryId = GuildChannelHelper.GuildCategoryId;
                            });
                            TargetGuild.ChannelId = newGuildChannel.Id;
                            saveChanges = true;
                            successful.Add(action);
                        }
                        else
                        {
                            errors.Add($"`{action}` - Could not find new guild channel!");
                        }
                        break;
                    case GuildModifyActions.setrole:
                        if (hasGuildContext)
                        {
                            if (ArgumentParsing.TryParseRole(guildContext, action.Argument, out SocketRole newGuildRole))
                            {
                                await newGuildRole.ModifyAsync(RoleProperties =>
                                {
                                    RoleProperties.Name = TargetGuild.Name;
                                    RoleProperties.Color = TargetGuild.DiscordColor;
                                });
                                TargetGuild.RoleId = newGuildRole.Id;
                                saveChanges = true;
                                successful.Add(action);
                            }
                            else
                            {
                                errors.Add($"`{action}` - Could not find new guild role!");
                            }
                        }
                        else
                        {
                            errors.Add($"`{action}` - This action can only be executed in a guild context");
                        }
                        break;
                    case GuildModifyActions.rename:
                        {
                            if (await MinecraftGuildModel.UpdateGuildNameAsync(TargetGuild, action.Argument))
                            {
                                successful.Add(action);
                                saveChanges = true;
                            }
                            else
                            {
                                errors.Add($"`{action}` - Internal error changing guild name");
                            }
                        }
                        break;
                    case GuildModifyActions.recolor:
                        if (Enum.TryParse(action.Argument, out GuildColor newColor))
                        {
                            if (await MinecraftGuildModel.UpdateGuildColorAsync(TargetGuild, newColor))
                            {
                                successful.Add(action);
                                saveChanges = true;
                            }
                            else
                            {
                                errors.Add($"`{action}` - Internal error changing guild color");
                            }
                        }
                        else
                        {
                            errors.Add($"`{action}` - Unable to parse {action.Argument} to a minecraft guild color!");
                        }
                        break;
                    case GuildModifyActions.setcaptain:
                        if (hasGuildContext)
                        {
                            if (ArgumentParsing.TryParseGuildUser(guildContext, action.Argument, out SocketGuildUser newCaptain))
                            {
                                if (TargetGuild.CaptainId == newCaptain.Id)
                                {
                                    errors.Add($"`{action}` - The new captain is already captain of this guild!");
                                }
                                else if (!TargetGuild.MemberIds.Contains(newCaptain.Id) && !TargetGuild.MateIds.Contains(newCaptain.Id))
                                {
                                    errors.Add($"`{action}` - The new captain has to be a member of this guild!");
                                }
                                else
                                {
                                    SocketGuildUser oldCaptain = guildContext.Guild.GetUser(TargetGuild.CaptainId);
                                    if (await MinecraftGuildModel.SetGuildCaptain(TargetGuild, newCaptain, oldCaptain))
                                    {
                                        saveChanges = true;
                                        successful.Add(action);
                                    }
                                    else
                                    {
                                        errors.Add($"`{action}` - Internal Error changing captain");
                                    }
                                }
                            }
                            else
                            {
                                errors.Add($"`{action}` - Could not find the new captain user!");
                            }
                        }
                        else
                        {
                            errors.Add($"`{action}` - This action can only be executed in a guild context");
                        }
                        break;
                    case GuildModifyActions.addmember:
                        if (hasGuildContext)
                        {
                            if (ArgumentParsing.TryParseGuildUser(guildContext, action.Argument, out SocketGuildUser newMember))
                            {
                                if (TargetGuild.MemberIds.Contains(newMember.Id))
                                {
                                    errors.Add($"`{action}` - This user is already member of this guild!");
                                }
                                else
                                {
                                    if (await MinecraftGuildModel.MemberJoinGuildAsync(TargetGuild, newMember))
                                    {
                                        successful.Add(action);
                                    }
                                    else
                                    {
                                        errors.Add($"`{action}` - An internal error occured while adding {newMember.Mention} to guild \"{TargetGuild.Name}\"!");
                                    }
                                }
                            }
                            else
                            {
                                errors.Add($"`{action}` - Could not find the new guild user!");
                            }
                        }
                        else
                        {
                            errors.Add($"`{action}` - This action can only be executed in a guild context");
                        }
                        break;
                    case GuildModifyActions.removemember:
                        if (hasGuildContext)
                        {
                            if (ArgumentParsing.TryParseGuildUser(guildContext, action.Argument, out SocketGuildUser leavingMember))
                            {
                                if (TargetGuild.CaptainId == leavingMember.Id)
                                {
                                    errors.Add($"`{action}` - Can not remove the guild captain! Assign a new guild captain first!");
                                }
                                else if (!TargetGuild.MemberIds.Contains(leavingMember.Id))
                                {
                                    errors.Add($"`{action}` - This user is not a member of this guild!");
                                }
                                else
                                {
                                    if (await MinecraftGuildModel.MemberLeaveGuildAsync(TargetGuild, leavingMember))
                                    {
                                        successful.Add(action);
                                    }
                                    else
                                    {
                                        errors.Add($"`{action}` - An internal error occured while removing {leavingMember.Mention} from guild \"{TargetGuild.Name}\"!");
                                    }
                                }
                            }
                            else
                            {
                                errors.Add($"`{action}` - Could not find the leaving guild user!");
                            }
                        }
                        else
                        {
                            errors.Add($"`{action}` - This action can only be executed in a guild context");
                        }
                        break;
                    case GuildModifyActions.timestamp:
                        if (DateTimeOffset.TryParseExact(action.Argument, "u", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out TargetGuild.FoundingTimestamp))
                        {
                            saveChanges = true;
                            successful.Add(action);
                        }
                        else
                        {
                            errors.Add($"`{action}` - Unable to parse to a valid ISO 8601 UTC timestamp!");
                        }
                        break;
                }
            }
            if (saveChanges)
            {
                await MinecraftGuildModel.SaveAll();
            }
            StringBuilder description = new StringBuilder();
            if (successful.Count > 0)
            {
                description.AppendLine("**Successful Actions**");
                foreach (GuildAction action in successful)
                {
                    description.AppendLine(Markdown.InlineCodeBlock(action));
                }
            }
            if (errors.Count > 0)
            {
                description.AppendLine("**Failed Actions**");
                foreach (string error in errors)
                {
                    description.AppendLine(error);
                }
            }
            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = (successful.Count == 0 ? BotCore.ErrorColor : BotCore.EmbedColor),
                Description = description.ToString()
            };
            if (successful.Count == 0)
            {
                embed.Title = "Failure";
            }
            else if (errors.Count == 0)
            {
                embed.Title = "Success";
            }
            else
            {
                embed.Title = "Partial Success";
            }
            await context.Channel.SendEmbedAsync(embed);
        }
    }

    #endregion
    #region info

    class GuildInfoCommand : Command
    {
        public const string SUMMARY = "Shows public info on all or one individual guild";
        public const string REMARKS = "If no Name is supplied, will display a list of all guilds";
        public const string LINK = "https://docs.google.com/document/d/1IdTQoq2l9YhF5Tlj5lBYz5Zcz56NQEgL3Hg5Dg2RyWs/edit#heading=h.bz5kjsmanwmo";
        public static readonly Argument[] ARGS = new Argument[] { new Argument("Name", "Name of the guild to get info on", true, true) };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.MINECRAFT };

        public GuildInfoCommand(string identifier) : base(identifier, OverriddenMethod.BasicSynchronous, OverriddenMethod.BasicAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        private MinecraftGuild TargetGuild;

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            if (context.Arguments.Count == 0)
            {
                TargetGuild = null;
            }
            else if (!ArgumentParsing.TryParseMinecraftGuild(context.Arguments, out string parsedName, out TargetGuild))
            {
                return new ArgumentParseResult(ARGS[0], $"Unable to find a guild named `{parsedName}`");
            }
            else if (!TargetGuild.Active)
            {
                return new ArgumentParseResult(ARGS[0], "This guild is currently inactive");
            }


            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            bool hasGuildContext = GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext);
            EmbedBuilder embed;


            if (TargetGuild != null)
            {
                embed = new EmbedBuilder()
                {
                    Color = TargetGuild.DiscordColor,
                    Title = $"Guild \"{TargetGuild.Name}\"",
                };
                if (TargetGuild.FoundingTimestamp == DateTimeOffset.MinValue)
                {
                    embed.Description = $"Color: `{TargetGuild.Color}`\nNo foundation timestamp!";
                }
                else
                {
                    embed.Description = $"Color: `{TargetGuild.Color}`\nFounded: `{TargetGuild.FoundingTimestamp.ToString("u")}`";
                }
                StringBuilder members = new StringBuilder();
                members.AppendLine("**Captain**");
                SocketUser guildCaptain = BotCore.Client.GetUser(TargetGuild.CaptainId);
                if (guildCaptain != null)
                {
                    members.AppendLine(guildCaptain.Mention);
                }
                else
                {
                    members.AppendLine(Markdown.InlineCodeBlock(TargetGuild.CaptainId));
                }
                members.AppendLine();
                members.AppendFormat("**Mates - {0}**\n", TargetGuild.MateIds.Count);
                foreach (ulong mateId in TargetGuild.MateIds)
                {
                    SocketUser mate = BotCore.Client.GetUser(mateId);
                    if (mate != null)
                    {
                        members.AppendLine(mate.Mention);
                    }
                    else
                    {
                        members.AppendLine(Markdown.InlineCodeBlock(mateId));
                    }
                }
                members.AppendLine();
                members.AppendFormat("**Members - {0}**\n", TargetGuild.MemberIds.Count);
                foreach (ulong memberId in TargetGuild.MemberIds)
                {
                    SocketUser member = BotCore.Client.GetUser(memberId);
                    if (member != null)
                    {
                        members.AppendLine(member.Mention);
                    }
                    else
                    {
                        members.AppendLine(Markdown.InlineCodeBlock(memberId));
                    }
                }
                embed.AddField("Members - " + TargetGuild.Count, members);
                StringBuilder info = new StringBuilder();
                info.Append("Channel: ");
                if (GuildChannelHelper.TryGetChannel(TargetGuild.ChannelId, out SocketTextChannel channel))
                {
                    info.AppendLine(channel.Mention);
                }
                else
                {
                    info.AppendLine(Markdown.InlineCodeBlock(TargetGuild.ChannelId));
                }
                info.Append("Role: ");
                if (BotCore.Client.TryGetRole(TargetGuild.RoleId, out SocketRole role))
                {
                    info.AppendLine(role.Mention);
                }
                else
                {
                    info.AppendLine(Markdown.InlineCodeBlock(TargetGuild.RoleId));
                }
                embed.AddField("Debug Information", info);

                await context.Channel.SendEmbedAsync(embed);
            }
            else
            {
                List<EmbedFieldBuilder> embeds = new List<EmbedFieldBuilder>();
                string title = "Guild List - " + MinecraftGuildModel.Guilds.Count;
                foreach (MinecraftGuild guild in MinecraftGuildModel.Guilds)
                {
                    if (guild.Active)
                    {
                        string name = "Guild Role Not Found!";
                        string color = "Guild Role Not Found!";
                        string captain;
                        if (guild.NameAndColorFound)
                        {
                            name = $"Guild \"{guild.Name}\"";
                            color = guild.Color.ToString();
                        }
                        SocketUser guildCaptain = BotCore.Client.GetUser(guild.CaptainId);
                        if (guildCaptain != null)
                        {
                            captain = guildCaptain.Mention;
                        }
                        else
                        {
                            captain = Markdown.InlineCodeBlock(guild.CaptainId);
                        }
                        embeds.Add(Macros.EmbedField(name, $"{color}, Captain: {captain}, {guild.MemberIds.Count + 1} Members"));
                    }
                }

                if (embeds.Count == 0)
                {
                    embed = new EmbedBuilder()
                    {
                        Title = "Guild List - 0",
                        Description = "No guilds found!"
                    };
                    await context.Channel.SendEmbedAsync(embed);
                }
                else
                {
                    await context.Channel.SendSafeEmbedList(title, embeds);
                }
            }

        }
    }

    #endregion
    #region invite

    class InviteMemberCommand : Command
    {
        public const string SUMMARY = "Invite members to join your guild";
        public const string REMARKS = "Only users who are not already in a guild and are part of the minecraft branch can be invited";
        public const string LINK = "https://docs.google.com/document/d/1IdTQoq2l9YhF5Tlj5lBYz5Zcz56NQEgL3Hg5Dg2RyWs/edit#heading=h.od6ln2j4yudz";
        public static readonly Argument[] ARGS = new Argument[] { new Argument("Member", "Users you want to invite to join your guild", multiple: true) };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.MINECRAFT, MinecraftGuildRankAuthPrecondition.MATE };

        public InviteMemberCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        private MinecraftGuild TargetGuild;
        private List<SocketGuildUser> newMembers = new List<SocketGuildUser>();
        private List<string> parseErrors = new List<string>();

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            bool captainOrMate = false;
            if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out TargetGuild))
            {
                captainOrMate = TargetGuild.CaptainId == context.User.Id || TargetGuild.MateIds.Contains(context.User.Id);
            }

            if (!captainOrMate)
            {
                return new ArgumentParseResult("This command requires you to be a captain or mate in a guild!");
            }

            if (!TargetGuild.Active)
            {
                return new ArgumentParseResult("Your guild is set to inactive! Contact an admin to reactivate it");
            }

            newMembers.Clear();
            parseErrors.Clear();
            for (; context.Arguments.Count > 0; context.Arguments.Index++)
            {
                if (ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out SocketGuildUser newMember, allowSelf: false))
                {
                    if (MinecraftGuildModel.TryGetGuildOfUser(newMember.Id, out MinecraftGuild existingGuild, true))
                    {
                        parseErrors.Add($"{newMember.Mention} is already in guild \"{(existingGuild.NameAndColorFound ? existingGuild.Name : existingGuild.ChannelId.ToString())}\"");
                    }
                    else
                    {
                        newMembers.Add(newMember);
                    }
                }
                else
                {
                    parseErrors.Add($"Unable to parse `{context.Arguments.First}` to a guild user!");
                }
            }
            if (newMembers.Count == 0)
            {
                return new ArgumentParseResult(ARGS[0], "Could not parse any of your arguments to members!\n" + string.Join('\n', parseErrors));
            }
            else
            {
                return ArgumentParseResult.SuccessfullParse;
            }
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            foreach (SocketGuildUser newMember in newMembers)
            {
                await GuildInvitationInteractiveMessage.CreateConfirmationMessage(TargetGuild, newMember, TargetGuild.DiscordColor);
            }

            if (parseErrors.Count > 0)
            {
                await context.Channel.SendEmbedAsync($"Invitation sent to: {string.Join(", ", newMembers)}\n\nFailed to parse some of the members:\n{string.Join('\n', parseErrors)}");
            }
            else
            {
                await context.Channel.SendEmbedAsync($"Invitation sent to: {string.Join(", ", newMembers)}");
            }
        }
    }

    #endregion
    #region kick

    class KickGuildMemberCommand : Command
    {
        public const string SUMMARY = "Kick members from your guild";
        public const string LINK = "https://docs.google.com/document/d/1IdTQoq2l9YhF5Tlj5lBYz5Zcz56NQEgL3Hg5Dg2RyWs/edit#heading=h.lqccs9cye6i3";
        public static readonly Argument[] ARGS = new Argument[] { new Argument("Member", "All members you want to have kicked from the guild. They can rejoin with a new invitation.", multiple: true) };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.MINECRAFT, MinecraftGuildRankAuthPrecondition.MATE };

        public KickGuildMemberCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, false, ARGS, AUTHCHECKS, SUMMARY, helplink:LINK)
        {
        }

        private MinecraftGuild TargetGuild;
        private List<SocketGuildUser> kickedMembers = new List<SocketGuildUser>();
        private List<string> parseErrors = new List<string>();

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            bool captainOrMate = false;
            if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out TargetGuild))
            {
                captainOrMate = TargetGuild.CaptainId == context.User.Id || TargetGuild.MateIds.Contains(context.User.Id);
            }

            if (!captainOrMate)
            {
                return new ArgumentParseResult("This command requires you to be a captain or mate in a guild!");
            }

            if (!TargetGuild.Active)
            {
                return new ArgumentParseResult("Your guild is set to inactive! Contact an admin to reactivate it");
            }

            kickedMembers.Clear();
            parseErrors.Clear();
            for (; context.Arguments.Count > 0; context.Arguments.Index++)
            {
                if (ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out SocketGuildUser kickedMember, allowSelf: false))
                {
                    if (kickedMember.Id == context.User.Id)
                    {
                        parseErrors.Add("Can not kick yourself!");
                    }
                    else
                    {
                        bool foundGuild = false;
                        if (MinecraftGuildModel.TryGetGuildOfUser(kickedMember.Id, out MinecraftGuild existingGuild, true))
                        {
                            if (existingGuild == TargetGuild)
                            {
                                if (TargetGuild.MemberIds.Contains(kickedMember.Id) || (TargetGuild.MateIds.Contains(kickedMember.Id) && TargetGuild.CaptainId == context.User.Id))
                                {
                                    foundGuild = true;
                                    kickedMembers.Add(kickedMember);
                                }
                            }
                        }

                        if (!foundGuild)
                        {
                            parseErrors.Add($"User {kickedMember.Mention} is not in a guild you manage!");
                        }

                    }
                }
                else
                {
                    parseErrors.Add($"Unable to parse `{context.Arguments.First}` to a guild user!");
                }
            }
            if (kickedMembers.Count == 0)
            {
                return new ArgumentParseResult(ARGS[0], "Could not parse any of your arguments to members!\n" + string.Join('\n', parseErrors));
            }
            else
            {
                return ArgumentParseResult.SuccessfullParse;
            }
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            StringBuilder kicked = new StringBuilder();
            for (int i = 0; i < kickedMembers.Count; i++)
            {
                SocketGuildUser kickedMember = kickedMembers[i];
                await MinecraftGuildModel.MemberLeaveGuildAsync(TargetGuild, kickedMember);

                kicked.Append(kickedMember.Mention);
                if (i < kickedMembers.Count - 1)
                {
                    kicked.Append(", ");
                }
            }

            if (parseErrors.Count > 0)
            {
                await context.Channel.SendEmbedAsync($"Kicked members: {kicked}\n\nFailed to parse some of the members:\n{string.Join('\n', parseErrors)}");
            }
            else
            {
                await context.Channel.SendEmbedAsync($"Kicked members: {kicked}");
            }
        }

    }

    #endregion
    #region leave

    class LeaveGuildCommand : Command
    {
        public const string SUMMARY = "Leave a guild";
        public const string REMARKS = "A captain can only leave their guild if no members are left, deleting it in the progress";
        public const string LINK = "https://docs.google.com/document/d/1IdTQoq2l9YhF5Tlj5lBYz5Zcz56NQEgL3Hg5Dg2RyWs/edit#heading=h.94kph266h8s2";
        public static readonly Argument[] ARGS = new Argument[] { };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.MINECRAFT };

        public LeaveGuildCommand(string identifier) : base(identifier, OverriddenMethod.None, OverriddenMethod.GuildAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            bool sendmessage = true;
            bool error = false;
            string message = string.Empty;
            if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out MinecraftGuild targetGuild, true))
            {
                if (targetGuild.MemberIds.Contains(context.User.Id) || targetGuild.MateIds.Contains(context.User.Id))
                {
                    if (targetGuild.Active)
                    {
                        await MinecraftGuildModel.MemberLeaveGuildAsync(targetGuild, context.GuildUser);
                    }
                    else
                    {
                        targetGuild.MemberIds.Remove(context.User.Id);
                        targetGuild.MateIds.Remove(context.User.Id);
                        await MinecraftGuildModel.SaveAll();
                    }
                    message = "Success!";
                }
                else if (targetGuild.MemberIds.Count == 0)
                {
                    await MinecraftGuildModel.DeleteGuildAsync(context.Guild, targetGuild);
                    message = "Guild Deleted!";
                    if (context.Channel.Id == targetGuild.ChannelId)
                    {
                        sendmessage = false;
                    }
                }
                else
                {
                    error = true;
                    message = "A captain can only leave a guild when no members are in it anymore. Pass the captain status or kick all members before attempting again";
                }
            }
            else
            {
                error = true;
                message = "You are not in a guild!";
            }

            if (sendmessage)
            {
                await context.Channel.SendEmbedAsync(message, error);
            }
        }
    }

    #endregion
    #region passcaptain

    class PassCaptainRightsCommand : Command
    {
        public const string SUMMARY = "Pass your captain position to another member";
        public const string REMARKS = default;
        public const string LINK = "https://docs.google.com/document/d/1IdTQoq2l9YhF5Tlj5lBYz5Zcz56NQEgL3Hg5Dg2RyWs/edit#heading=h.z9qkxx7wamoo";
        public static readonly Argument[] ARGS = new Argument[] { new Argument("Member", "The member you want to pass your captain position to") };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.MINECRAFT, MinecraftGuildRankAuthPrecondition.CAPTAIN };

        private MinecraftGuild TargetGuild;
        private SocketGuildUser NewCaptain;

        public PassCaptainRightsCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            bool ownsGuild = false;
            if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out TargetGuild))
            {
                ownsGuild = TargetGuild.CaptainId == context.User.Id;
            }

            if (!ownsGuild)
            {
                return new ArgumentParseResult("You are not a captain of a guild!");
            }

            if (!TargetGuild.Active)
            {
                return new ArgumentParseResult("Your guild is set to inactive! Contact an admin to reactivate it");
            }

            if (ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out NewCaptain))
            {
                if (NewCaptain.Id == context.User.Id)
                {
                    return new ArgumentParseResult(ARGS[0], "Can not pass captain rights to yourself!");
                }
                else if (!TargetGuild.MemberIds.Contains(NewCaptain.Id) && !TargetGuild.MateIds.Contains(NewCaptain.Id))
                {
                    return new ArgumentParseResult(ARGS[0], "Can not pass captain rights to a user not in your guild!");
                }
            }
            else
            {
                return new ArgumentParseResult(ARGS[0], "Could not parse to a valid user!");
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            if (await MinecraftGuildModel.SetGuildCaptain(TargetGuild, NewCaptain, context.GuildUser))
            {
                await context.Channel.SendEmbedAsync($"{context.GuildUser.Mention} passed on captain rights to {NewCaptain.Mention}!");
            }
            else
            {
                await context.Channel.SendEmbedAsync("Internal error passing captain rights", true);
            }
        }
    }

    #endregion
    #region promote

    class PromoteMateCommand : Command
    {
        public const string SUMMARY = "Promote a member of your guild to mate";
        public const string REMARKS = "Mates are able to invite and kick members of your guild";
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] { new Argument("Mate", "The member of your guild you want to promote to mate rank") };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.MINECRAFT, MinecraftGuildRankAuthPrecondition.CAPTAIN };

        public PromoteMateCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        private MinecraftGuild TargetGuild;
        private SocketGuildUser NewMate;

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            bool ownsGuild = false;
            if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out TargetGuild))
            {
                ownsGuild = TargetGuild.CaptainId == context.User.Id;
            }

            if (!ownsGuild)
            {
                return new ArgumentParseResult("You are not a captain of a guild!");
            }

            if (!TargetGuild.Active)
            {
                return new ArgumentParseResult("Your guild is set to inactive! Contact an admin to reactivate it");
            }

            if (ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out NewMate, allowSelf: false))
            {
                if (MinecraftGuildModel.TryGetGuildOfUser(NewMate.Id, out MinecraftGuild MateGuild))
                {
                    if (MateGuild == TargetGuild)
                    {
                        if (!MateGuild.MemberIds.Contains(NewMate.Id))
                        {
                            return new ArgumentParseResult(ARGS[0], "The user promoted to mate must be a regular guild member!");
                        }
                    }
                    else
                    {
                        return new ArgumentParseResult(ARGS[0], "The user you want to promote is not your guild");
                    }
                }
                else
                {
                    return new ArgumentParseResult(ARGS[0], "The user you want to promote is not your guild");
                }
            }
            else
            {
                return new ArgumentParseResult(ARGS[0], $"Could not parse {context.Arguments.First} to a user in this guild!");
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            if (await MinecraftGuildModel.PromoteGuildMember(TargetGuild, NewMate))
            {
                await context.Channel.SendEmbedAsync($"Successfully promoted {NewMate.Mention} to `Mate` rank!");
            }
            else
            {
                await context.Channel.SendEmbedAsync($"Failed to promote {NewMate.Mention} to `Mate` rank!", true);
            }
        }
    }

    #endregion
    #region demote

    class DemoteMateCommand : Command
    {
        public const string SUMMARY = "Demote a mate in your guild to a regular member";
        public const string REMARKS = default;
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] { new Argument("Mate", "The mate in your guild you want to demote to a regular member") };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.MINECRAFT, MinecraftGuildRankAuthPrecondition.CAPTAIN };

        public DemoteMateCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        private MinecraftGuild TargetGuild;
        private SocketGuildUser DemotedMate;

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            bool ownsGuild = false;
            if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out TargetGuild))
            {
                ownsGuild = TargetGuild.CaptainId == context.User.Id;
            }

            if (!ownsGuild)
            {
                return new ArgumentParseResult("You are not a captain of a guild!");
            }

            if (!TargetGuild.Active)
            {
                return new ArgumentParseResult("Your guild is set to inactive! Contact an admin to reactivate it");
            }

            if (ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out DemotedMate, allowSelf: false))
            {
                if (MinecraftGuildModel.TryGetGuildOfUser(DemotedMate.Id, out MinecraftGuild MateGuild))
                {
                    if (MateGuild == TargetGuild)
                    {
                        if (!MateGuild.MateIds.Contains(DemotedMate.Id))
                        {
                            return new ArgumentParseResult(ARGS[0], "The user demoted to regular member must be a mate!");
                        }
                    }
                    else
                    {
                        return new ArgumentParseResult(ARGS[0], "The user you want to demote is not your guild");
                    }
                }
                else
                {
                    return new ArgumentParseResult(ARGS[0], "The user you want to demote is not your guild");
                }
            }
            else
            {
                return new ArgumentParseResult(ARGS[0], $"Could not parse {context.Arguments.First} to a user in this guild!");
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            if (await MinecraftGuildModel.DemoteGuildMember(TargetGuild, DemotedMate))
            {
                await context.Channel.SendEmbedAsync($"Successfully demote {DemotedMate.Mention} to `Regular Member` rank!");
            }
            else
            {
                await context.Channel.SendEmbedAsync($"Failed to demote {DemotedMate.Mention} to `Mate` rank!", true);
            }
        }
    }

    #endregion
    #region sync

    class SyncGuildsCommand : Command
    {
        public const string SUMMARY = "Start the guild info syncing process";
        public const string REMARKS = "This command is used to sync data internally stored with discord data";
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] { new Argument("GuildId", "Discord Id of the guild (Discord Server) you want to sync against", true) };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public SyncGuildsCommand(string identifier) : base(identifier, OverriddenMethod.BasicSynchronous, OverriddenMethod.BasicAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        private SocketGuild DiscordGuild;

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            if (context.Arguments.Count == 0)
            {
                if (GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext))
                {
                    DiscordGuild = guildContext.Guild;
                }
                else
                {
                    return new ArgumentParseResult(ARGS[0], "This command can not be used without arguments in PMs");
                }
            }
            else
            {
                if (ulong.TryParse(context.Arguments.First, out ulong guildId))
                {
                    DiscordGuild = BotCore.Client.GetGuild(guildId);
                    if (DiscordGuild == null)
                    {
                        return new ArgumentParseResult(ARGS[0], $"Could not find a guild with id `{guildId}`");
                    }
                }
                else
                {
                    return new ArgumentParseResult(ARGS[0], $"Could not parse {context.Arguments.First} to a valid discord Id");
                }
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            List<DesyncItem> problems = new List<DesyncItem>();

            List<ulong> userIds = new List<ulong>(DiscordGuild.Users.Select(user => { return user.Id; }));
            List<Tuple<SocketRole, MinecraftGuild>> roles = new List<Tuple<SocketRole, MinecraftGuild>>();

            // Missing Guild Channels and Roles

            foreach (MinecraftGuild guild in MinecraftGuildModel.Guilds)
            {
                guild.TryFindNameAndColor();
                bool channelFound = DiscordGuild.GetTextChannel(guild.ChannelId) != null;
                SocketRole guildRole = DiscordGuild.GetRole(guild.RoleId);
                bool roleFound = guildRole != null;

                if (!channelFound)
                {
                    // Channel missing
                    problems.Add(new DesyncItem("Channel Not Found", $"The channel of guild \"{guild.Name}\" couldn't be located. Suggested action: Assign new channel with `/guild modify {guild.Name_CommandSafe} setchannel:<NewChannel>`", new DeleteGuildDatasetOption(guild)));
                }
                if (!roleFound)
                {
                    // role missing
                    problems.Add(new DesyncItem("Role Not Found", $"The role of guild \"{guild.Name}\" couldn't be located (guild names can not be loaded without roles!). Suggested action: Assign new role with `/guild modify {guild.Name_CommandSafe} setrole:<NewRole>`", new DeleteGuildDatasetOption(guild)));
                }
                else
                {
                    roles.Add(new Tuple<SocketRole, MinecraftGuild>(guildRole, guild));
                }
            }

            // Missing Users

            foreach (MinecraftGuild guild in MinecraftGuildModel.Guilds)
            {
                SocketRole guildRole = roles.Find((Tuple<SocketRole, MinecraftGuild> tuple) => { return tuple.Item2 == guild; })?.Item1;

                if (!userIds.Contains(guild.CaptainId))
                {
                    // Captain Missing!
                    problems.Add(new DesyncItem("Captain Not Found", $"Captain (ID: `{guild.CaptainId}`, DebugMention: {Markdown.Mention_User(guild.CaptainId)}) of Guild \"{guild.Name}\" missing! Recommended action: Reassign new captain with command `/guild modify {guild.Name_CommandSafe} setcaptain:<NewCaptain>`"));
                }
                else if ((guildRole != null) && !guildRole.Members.Any((SocketGuildUser user) => { return user.Id == guild.CaptainId; }))
                {
                    // Captain does not have guild role
                    problems.Add(new DesyncItem("Captain without guild role", $"Captain {Markdown.Mention_User(guild.CaptainId)} of Guild \"{guild.Name}\" does not have a guild role!", new AddRoleOption(DiscordGuild.GetUser(guild.CaptainId), guildRole), new DeleteGuildDatasetOption(guild)));
                }
                foreach (ulong mateId in guild.MateIds)
                {
                    if (!userIds.Contains(mateId))
                    {
                        // Mate Missing!
                        problems.Add(new DesyncItem("Mate Member Not Found", $"Mate (ID: `{mateId}`, DebugMention: {Markdown.Mention_User(mateId)}) of Guild \"{guild.Name}\" missing!",
                            new RemoveMemberDatasetDesyncOption(guild, mateId)));
                    }
                    else if ((guildRole != null) && !guildRole.Members.Any((SocketGuildUser user) => { return user.Id == mateId; }))
                    {
                        // Mate does not have guild role
                        problems.Add(new DesyncItem("Mate without guild role", $"Mate {Markdown.Mention_User(mateId)} of Guild \"{guild.Name}\" does not have a guild role!", new AddRoleOption(DiscordGuild.GetUser(mateId), guildRole), new RemoveMemberDatasetDesyncOption(guild, mateId)));
                    }
                }
                foreach (ulong memberId in guild.MemberIds)
                {
                    if (!userIds.Contains(memberId))
                    {
                        // Member Missing!
                        problems.Add(new DesyncItem("Member Not Found", $"Member (ID: `{memberId}`, DebugMention: {Markdown.Mention_User(memberId)}) of Guild \"{guild.Name}\" missing!",
                            new RemoveMemberDatasetDesyncOption(guild, memberId)));
                    }
                    else if ((guildRole != null) && !guildRole.Members.Any((SocketGuildUser user) => { return user.Id == memberId; }))
                    {
                        problems.Add(new DesyncItem("Mate without guild role", $"Mate {Markdown.Mention_User(memberId)} of Guild \"{guild.Name}\" does not have a guild role!", new AddRoleOption(DiscordGuild.GetUser(memberId), guildRole), new RemoveMemberDatasetDesyncOption(guild, memberId)));
                        // Mate does not have guild role
                    }
                }
            }

            // People wearing roles but not part of a guild

            foreach (Tuple<SocketRole, MinecraftGuild> roleguildtuple in roles)
            {
                foreach (var user in roleguildtuple.Item1.Members)
                {
                    if (!roleguildtuple.Item2.UserIsInGuild(user.Id))
                    {
                        // User has guild role but is not in guild!
                        problems.Add(new DesyncItem("User with Guild role is not in guild", $"User {user.Mention} has role {roleguildtuple.Item1.Mention}, but is not listed as a member", new AddUserToDatasetOption(roleguildtuple.Item2, user.Id), new RemoveRoleOption(user, roleguildtuple.Item1)));
                    }
                }
            }

            if (problems.Count == 0)
            {
                await context.Channel.SendEmbedAsync("No desync problems found!");
            }
            else
            {
                await context.Channel.SendEmbedAsync($"Found {problems.Count} desync problems!");
                await GuildDesyncInteractiveMessage.Create(context.Channel, problems);
            }
        }
    }

    #endregion
}
