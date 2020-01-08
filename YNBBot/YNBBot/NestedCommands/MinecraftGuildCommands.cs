using BotCoreNET;
using BotCoreNET.BotVars;
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
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;

        public override string Summary => "Requests creation of a new minecraft guild";
        public override string Link => "https://docs.google.com/document/d/1IdTQoq2l9YhF5Tlj5lBYz5Zcz56NQEgL3Hg5Dg2RyWs/edit#heading=h.h41js19sf4v4";
        private static readonly Argument[] ARGS = new Argument[] {
            new Argument("Name", "The name of the guild. Will be the name of the channel and role created. Also applies to ingame naming"),
            new Argument("Color", $"The color of the guild. Will be the color of the role created. Also applies to ingame color. Available are `{string.Join(", ", MinecraftGuildSystem.MinecraftGuildModel.AvailableColors)}`"),
            new Argument("Members", "Minimum of two members, selected either by discord snowflake id, mention or Username#Discriminator", multiple: true)};
        public override Argument[] Arguments => ARGS;
        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(Var.MinecraftBranchRoleBotVarId) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;
        public override string Remarks => $"{ARGS[0]} and {ARGS[1]} have to be free to take, all invited members ({ARGS[2]}) have to accept the invitation, and an admin has to confirm the creation of the new guild";

        public override bool RunInAsyncMode => true;

        public CreateGuildCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public string GuildName;
            public GuildColor GuildColor;
            public List<SocketGuildUser> Members = new List<SocketGuildUser>();
        }


        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer args = new ArgumentContainer();

            if (MinecraftGuildSystem.MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out MinecraftGuild contextUserGuild))
            {
                if (contextUserGuild.Active)
                {
                    return Task.FromResult(new ArgumentParseResult($"You can not found a new guild because you are still part of `{contextUserGuild.Name}`"));
                }
                else if (contextUserGuild.CaptainId == context.User.Id)
                {
                    return Task.FromResult(new ArgumentParseResult($"You can not found a new guild because you captain of the inactive guild `{contextUserGuild.Name}`. Please contact an admin!"));
                }
            }

            args.GuildName = context.Arguments[0];


            if (args.GuildName.Length < 3)
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[0], "Too short! Minimum of 3 Characters"));
            }
            if (args.GuildName.Length > 50)
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[0], "Too long! Maximum of 50 Characters"));
            }
            if (!MinecraftGuildModel.NameIsLegal(args.GuildName))
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[0], "The guild name contains illegal characters! (Or starts/ends with a whitespace)"));
            }
            if (!MinecraftGuildModel.NameIsAvailable(args.GuildName))
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[0], "A guild with this name already exists!"));
            }

            context.Arguments.Index++;

            if (!Enum.TryParse(context.Arguments[0], out args.GuildColor))
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[1], $"Could not parse to an available guild color! Available are `{string.Join(", ", MinecraftGuildModel.AvailableColors)}`"));
            }
            if (!MinecraftGuildModel.ColorIsAvailable(args.GuildColor))
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[1], $"A guild with this color already exists! Available are `{string.Join(", ", MinecraftGuildModel.AvailableColors)}`"));
            }

            context.Arguments.Index++;

            if (context.Arguments.Count < MinecraftGuildModel.MIN_GUILDFOUNDINGMEMBERS)
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[2], $"You need to supply a minimum of {MinecraftGuildModel.MIN_GUILDFOUNDINGMEMBERS} members!"));
            }

            BotVarCollection guildBotVars = BotVarManager.GetGuildBotVarCollection(context.Guild.Id);
            if (!guildBotVars.TryGetBotVar(Var.MinecraftBranchRoleBotVarId, out ulong minecraftBranchRoleId))
            {
                return Task.FromResult(new ArgumentParseResult($"Minecraft Branch role not assigned in guild botvars!"));
            }

            for (int i = 0; i < context.Arguments.Count; i++)
            {
                if (ArgumentParsing.TryParseGuildUser(context, context.Arguments[i], out SocketGuildUser member, allowSelf: false))
                {
                    if (member.Id == context.User.Id)
                    {
                        return Task.FromResult(new ArgumentParseResult(ARGS[2], "Can not add yourself as a guild member!"));
                    }
                    if (MinecraftGuildSystem.MinecraftGuildModel.TryGetGuildOfUser(member.Id, out MinecraftGuild memberGuild))
                    {
                        if (memberGuild.Active)
                        {
                            return Task.FromResult(new ArgumentParseResult(ARGS[2], $"Can not invite {member.Mention}, because they are already part of \"{memberGuild.Name}\""));
                        }
                        else if (memberGuild.CaptainId == member.Id)
                        {
                            return Task.FromResult(new ArgumentParseResult(ARGS[2], $"Can not invite {member.Mention}, because they are captain of inactivated guild \"{memberGuild.Name}\". Please contact an admin!"));
                        }
                    }
                    bool hasMinecraftRole = false;
                    foreach (SocketRole role in member.Roles)
                    {
                        if (role.Id == minecraftBranchRoleId)
                        {
                            hasMinecraftRole = true;
                            break;
                        }
                    }

                    if (!hasMinecraftRole)
                    {
                        return Task.FromResult(new ArgumentParseResult(ARGS[2], $"Can not invite {member.Mention}, because they are not part of the minecraft branch!"));
                    }

                    args.Members.Add(member);
                }
                else
                {
                    return Task.FromResult(new ArgumentParseResult(ARGS[2], $"Could not parse `{context.Arguments[i]}` to a guild user!"));
                }
            }

            return Task.FromResult(new ArgumentParseResult(args));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            MinecraftGuild guild = new MinecraftGuild()
            {
                Name = args.GuildName,
                Color = args.GuildColor,
                CaptainId = context.User.Id,
                MemberIds = new List<ulong>()
            };

            await GuildCreationInteractiveMessage.FromNewGuildAndMemberList(guild, args.Members);
        }
    }

    #endregion
    #region modify

    class ModifyGuildCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;

        public override string Summary => SUMMARY;
        public override string Remarks => REMARKS;
        public override Argument[] Arguments => ARGS;
        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(BotCore.ADMINROLE_BOTVARID) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;

        public override bool RunInAsyncMode => true;

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
        public const string LINK = "https://docs.google.com/document/d/1VFWKTcdHxARXMvaSZCceFVCXZVqWpMQyBT8EZrLRoRA/edit#heading=h.z9qkxx7wamoo";

        public ModifyGuildCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
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

        private class ArgumentContainer
        {
            public List<GuildAction> Actions = new List<GuildAction>();
            public MinecraftGuild TargetGuild;
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer args = new ArgumentContainer();

            if (!MinecraftGuildModel.TryParseMinecraftGuild(context.Arguments.First, out args.TargetGuild))
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[0], $"Unable to find a guild named `{context.Arguments.First}`"));
            }

            context.Arguments.Index++;

            GuildModifyActions action = GuildModifyActions.delete;
            string arg = string.Empty;
            for (; context.Arguments.Count > 0; context.Arguments.Index++)
            {
                string current = context.Arguments.First;
                string identifier;
                string argument;
                int index = current.IndexOf(':');
                if (index == -1)
                {
                    identifier = current;
                    argument = string.Empty;
                }
                else
                {
                    identifier = current.Substring(0, index);
                    if (index + 1 >= current.Length)
                    {
                        argument = string.Empty;
                    }
                    else
                    {
                        argument = current.Substring(index + 1);
                    }
                }

                if (!Enum.TryParse(identifier, true, out action))
                {
                    return Task.FromResult(new ArgumentParseResult(ARGS[1], $"Could not parse `{identifier}` to a GuildModifyAction!"));
                }

                if (ActionRequiresArg[(int)action] == string.IsNullOrEmpty(argument))
                {
                    return Task.FromResult(new ArgumentParseResult(ARGS[1], $"The GuildModifyAction `{action}` requires {(ActionRequiresArg[(int)action] ? "an argument" : "no arguments")}"));
                }

                args.Actions.Add(new GuildAction(action, argument));
            }

            return Task.FromResult(new ArgumentParseResult(args));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            List<string> errors = new List<string>();
            List<GuildAction> successful = new List<GuildAction>();

            bool saveChanges = false;
            args.Actions.Sort();
            for (int i = 0; i < args.Actions.Count; i++)
            {
                GuildAction action = args.Actions[i];
                switch (action.Action)
                {
                    case GuildModifyActions.delete:
                        if (await MinecraftGuildModel.DeleteGuildAsync(context.Guild, args.TargetGuild))
                        {
                            successful.Add(action);
                        }
                        else
                        {
                            errors.Add($"`{action}` - Internal Error deleting Guild!");
                        }
                        i = args.Actions.Count;
                        break;
                    case GuildModifyActions.deletedataset:
                        await MinecraftGuildModel.DeleteGuildDatasetAsync(args.TargetGuild);
                        i = args.Actions.Count;
                        break;
                    case GuildModifyActions.setactive:
                        switch (action.Argument.ToLower())
                        {
                            case "active":
                            case "true":
                                if (!args.TargetGuild.Active)
                                {
                                    if (await MinecraftGuildModel.SetGuildActive(context.Guild, args.TargetGuild, true))
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
                                if (args.TargetGuild.Active)
                                {
                                    if (await MinecraftGuildModel.SetGuildActive(context.Guild, args.TargetGuild, false))
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
                        i = args.Actions.Count;
                        break;
                    case GuildModifyActions.setchannel:
                        if (ArgumentParsing.TryParseGuildTextChannel(context, action.Argument, out SocketTextChannel newGuildChannel))
                        {
                            await newGuildChannel.ModifyAsync(GuildChannelProperties =>
                            {
                                GuildChannelProperties.Name = args.TargetGuild.Name;
                                GuildChannelProperties.CategoryId = GuildChannelHelper.GuildCategoryId;
                            });
                            args.TargetGuild.ChannelId = newGuildChannel.Id;
                            saveChanges = true;
                            successful.Add(action);
                        }
                        else
                        {
                            errors.Add($"`{action}` - Could not find new guild channel!");
                        }
                        break;
                    case GuildModifyActions.setrole:
                        if (ArgumentParsing.TryParseRole(context, action.Argument, out SocketRole newGuildRole))
                        {
                            await newGuildRole.ModifyAsync(RoleProperties =>
                            {
                                RoleProperties.Name = args.TargetGuild.Name;
                                RoleProperties.Color = args.TargetGuild.DiscordColor;
                            });
                            args.TargetGuild.RoleId = newGuildRole.Id;
                            saveChanges = true;
                            successful.Add(action);
                        }
                        else
                        {
                            errors.Add($"`{action}` - Could not find new guild role!");
                        }
                        break;
                    case GuildModifyActions.rename:
                        {
                            if (await MinecraftGuildModel.UpdateGuildNameAsync(args.TargetGuild, action.Argument))
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
                            if (await MinecraftGuildModel.UpdateGuildColorAsync(args.TargetGuild, newColor))
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
                        if (ArgumentParsing.TryParseGuildUser(context, action.Argument, out SocketGuildUser newCaptain))
                        {
                            if (args.TargetGuild.CaptainId == newCaptain.Id)
                            {
                                errors.Add($"`{action}` - The new captain is already captain of this guild!");
                            }
                            else if (!args.TargetGuild.MemberIds.Contains(newCaptain.Id) && !args.TargetGuild.MateIds.Contains(newCaptain.Id))
                            {
                                errors.Add($"`{action}` - The new captain has to be a member of this guild!");
                            }
                            else
                            {
                                SocketGuildUser oldCaptain = context.Guild.GetUser(args.TargetGuild.CaptainId);
                                if (await MinecraftGuildModel.SetGuildCaptain(args.TargetGuild, newCaptain, oldCaptain))
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
                            errors.Add($"`{action}` - This action can only be executed in a guild context");
                        }
                        break;
                    case GuildModifyActions.addmember:
                        if (ArgumentParsing.TryParseGuildUser(context, action.Argument, out SocketGuildUser newMember))
                        {
                            if (args.TargetGuild.MemberIds.Contains(newMember.Id))
                            {
                                errors.Add($"`{action}` - This user is already member of this guild!");
                            }
                            else
                            {
                                if (await MinecraftGuildModel.MemberJoinGuildAsync(args.TargetGuild, newMember))
                                {
                                    successful.Add(action);
                                }
                                else
                                {
                                    errors.Add($"`{action}` - An internal error occured while adding {newMember.Mention} to guild \"{args.TargetGuild.Name}\"!");
                                }
                            }
                        }
                        else
                        {
                            errors.Add($"`{action}` - Could not find the new guild user!");
                        }
                        break;
                    case GuildModifyActions.removemember:
                        if (ArgumentParsing.TryParseGuildUser(context, action.Argument, out SocketGuildUser leavingMember))
                        {
                            if (args.TargetGuild.CaptainId == leavingMember.Id)
                            {
                                errors.Add($"`{action}` - Can not remove the guild captain! Assign a new guild captain first!");
                            }
                            else if (!args.TargetGuild.MemberIds.Contains(leavingMember.Id))
                            {
                                errors.Add($"`{action}` - This user is not a member of this guild!");
                            }
                            else
                            {
                                if (await MinecraftGuildModel.MemberLeaveGuildAsync(args.TargetGuild, leavingMember))
                                {
                                    successful.Add(action);
                                }
                                else
                                {
                                    errors.Add($"`{action}` - An internal error occured while removing {leavingMember.Mention} from guild \"{args.TargetGuild.Name}\"!");
                                }
                            }
                        }
                        else
                        {
                            errors.Add($"`{action}` - Could not find the leaving guild user!");
                        }
                        break;
                    case GuildModifyActions.timestamp:
                        if (DateTimeOffset.TryParseExact(action.Argument, "u", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out args.TargetGuild.FoundingTimestamp))
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
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;
        public override string Summary => SUMMARY;
        public override string Remarks => REMARKS;
        public override string Link => LINK;
        public override Argument[] Arguments => ARGS;
        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(Var.MinecraftBranchRoleBotVarId) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;
        public const string SUMMARY = "Shows public info on all or one individual guild";
        public const string REMARKS = "If no Name is supplied, will display a list of all guilds";
        public const string LINK = "https://docs.google.com/document/d/1IdTQoq2l9YhF5Tlj5lBYz5Zcz56NQEgL3Hg5Dg2RyWs/edit#heading=h.bz5kjsmanwmo";
        public static readonly Argument[] ARGS = new Argument[] { new Argument("Name", "Name of the guild to get info on", true) };

        public GuildInfoCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }


        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            MinecraftGuild targetGuild;
            if (context.Arguments.Count == 0)
            {
                targetGuild = null;
            }
            else if (!MinecraftGuildModel.TryParseMinecraftGuild(context.Arguments.First, out targetGuild))
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[0], $"Unable to find a guild named `{context.Arguments.First}`"));
            }
            else if (!targetGuild.Active)
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[0], "This guild is currently inactive"));
            }


            return Task.FromResult(new ArgumentParseResult(targetGuild));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            MinecraftGuild targetGuild = argObj as MinecraftGuild;

            EmbedBuilder embed;

            if (targetGuild != null)
            {
                embed = new EmbedBuilder()
                {
                    Color = targetGuild.DiscordColor,
                    Title = $"Guild \"{targetGuild.Name}\"",
                };
                if (targetGuild.FoundingTimestamp == DateTimeOffset.MinValue)
                {
                    embed.Description = $"Color: `{targetGuild.Color}`\nNo foundation timestamp!";
                }
                else
                {
                    embed.Description = $"Color: `{targetGuild.Color}`\nFounded: `{targetGuild.FoundingTimestamp.ToString("u")}`";
                }
                StringBuilder members = new StringBuilder();
                members.AppendLine("**Captain**");
                SocketUser guildCaptain = BotCore.Client.GetUser(targetGuild.CaptainId);
                if (guildCaptain != null)
                {
                    members.AppendLine(guildCaptain.Mention);
                }
                else
                {
                    members.AppendLine(Markdown.InlineCodeBlock(targetGuild.CaptainId));
                }
                members.AppendLine();
                members.AppendFormat("**Mates - {0}**\n", targetGuild.MateIds.Count);
                foreach (ulong mateId in targetGuild.MateIds)
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
                members.AppendFormat("**Members - {0}**\n", targetGuild.MemberIds.Count);
                foreach (ulong memberId in targetGuild.MemberIds)
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
                embed.AddField("Members - " + targetGuild.Count, members);
                StringBuilder info = new StringBuilder();
                info.Append("Channel: ");
                if (GuildChannelHelper.TryGetChannel(targetGuild.ChannelId, out SocketTextChannel channel))
                {
                    info.AppendLine(channel.Mention);
                }
                else
                {
                    info.AppendLine(Markdown.InlineCodeBlock(targetGuild.ChannelId));
                }
                info.Append("Role: ");
                if (BotCore.Client.TryGetRole(targetGuild.RoleId, out SocketRole role))
                {
                    info.AppendLine(role.Mention);
                }
                else
                {
                    info.AppendLine(Markdown.InlineCodeBlock(targetGuild.RoleId));
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
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;
        public override string Summary => SUMMARY;
        public override string Remarks => REMARKS;
        public override string Link => LINK;
        public override Argument[] Arguments => ARGS;
        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(Var.MinecraftBranchRoleBotVarId), new MinecraftGuildRankPrecondition(GuildRank.Mate) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;
        public override bool RunInAsyncMode => true;
        public const string SUMMARY = "Invite members to join your guild";
        public const string REMARKS = "Only users who are not already in a guild and are part of the minecraft branch can be invited";
        public const string LINK = "https://docs.google.com/document/d/1IdTQoq2l9YhF5Tlj5lBYz5Zcz56NQEgL3Hg5Dg2RyWs/edit#heading=h.od6ln2j4yudz";
        public static readonly Argument[] ARGS = new Argument[] { new Argument("Member", "Users you want to invite to join your guild", multiple: true) };

        public InviteMemberCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public MinecraftGuild TargetGuild;
            public List<SocketGuildUser> newMembers = new List<SocketGuildUser>();
            public List<string> parseErrors = new List<string>();
        }


        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer args = new ArgumentContainer();

            bool captainOrMate = false;
            if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out args.TargetGuild))
            {
                captainOrMate = args.TargetGuild.CaptainId == context.User.Id || args.TargetGuild.MateIds.Contains(context.User.Id);
            }

            if (!captainOrMate)
            {
                return Task.FromResult(new ArgumentParseResult("This command requires you to be a captain or mate in a guild!"));
            }

            if (!args.TargetGuild.Active)
            {
                return Task.FromResult(new ArgumentParseResult("Your guild is set to inactive! Contact an admin to reactivate it"));
            }

            for (; context.Arguments.Count > 0; context.Arguments.Index++)
            {
                if (ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out SocketGuildUser newMember, allowSelf: false))
                {
                    if (MinecraftGuildModel.TryGetGuildOfUser(newMember.Id, out MinecraftGuild existingGuild, true))
                    {
                        args.parseErrors.Add($"{newMember.Mention} is already in guild \"{(existingGuild.NameAndColorFound ? existingGuild.Name : existingGuild.ChannelId.ToString())}\"");
                    }
                    else
                    {
                        args.newMembers.Add(newMember);
                    }
                }
                else
                {
                    args.parseErrors.Add($"Unable to parse `{context.Arguments.First}` to a guild user!");
                }
            }
            if (args.newMembers.Count == 0)
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[0], "Could not parse any of your arguments to members!\n" + string.Join('\n', args.parseErrors)));
            }
            else
            {
                return Task.FromResult(new ArgumentParseResult(args));
            }
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            foreach (SocketGuildUser newMember in args.newMembers)
            {
                await GuildInvitationInteractiveMessage.CreateConfirmationMessage(args.TargetGuild, newMember, args.TargetGuild.DiscordColor);
            }

            if (args.parseErrors.Count > 0)
            {
                await context.Channel.SendEmbedAsync($"Invitation sent to: {string.Join(", ", args.newMembers)}\n\nFailed to parse some of the members:\n{string.Join('\n', args.parseErrors)}");
            }
            else
            {
                await context.Channel.SendEmbedAsync($"Invitation sent to: {string.Join(", ", args.newMembers)}");
            }
        }
    }

    #endregion
    #region kick

    class KickGuildMemberCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;
        public override string Summary => SUMMARY;
        public override string Link => LINK;
        public override Argument[] Arguments => ARGS;
        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(Var.MinecraftBranchRoleBotVarId), new MinecraftGuildRankPrecondition(GuildRank.Mate) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;
        public override bool RunInAsyncMode => true;
        public const string SUMMARY = "Kick members from your guild";
        public const string LINK = "https://docs.google.com/document/d/1IdTQoq2l9YhF5Tlj5lBYz5Zcz56NQEgL3Hg5Dg2RyWs/edit#heading=h.lqccs9cye6i3";
        public static readonly Argument[] ARGS = new Argument[] { new Argument("Member", "All members you want to have kicked from the guild. They can rejoin with a new invitation.", multiple: true) };

        public KickGuildMemberCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public MinecraftGuild TargetGuild;
            public List<SocketGuildUser> kickedMembers = new List<SocketGuildUser>();
            public List<string> parseErrors = new List<string>();
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer args = new ArgumentContainer();

            bool captainOrMate = false;
            if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out args.TargetGuild))
            {
                captainOrMate = args.TargetGuild.GetMemberRank(context.User.Id) >= GuildRank.Mate;
            }

            if (!captainOrMate)
            {
                return Task.FromResult(new ArgumentParseResult("This command requires you to be a captain or mate in a guild!"));
            }

            if (!args.TargetGuild.Active)
            {
                return Task.FromResult(new ArgumentParseResult("Your guild is set to inactive! Contact an admin to reactivate it"));
            }

            for (; context.Arguments.Count > 0; context.Arguments.Index++)
            {
                if (ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out SocketGuildUser kickedMember, allowSelf: false))
                {
                    if (kickedMember.Id == context.User.Id)
                    {
                        args.parseErrors.Add("Can not kick yourself!");
                    }
                    else
                    {
                        bool foundGuild = false;
                        if (MinecraftGuildModel.TryGetGuildOfUser(kickedMember.Id, out MinecraftGuild existingGuild, true))
                        {
                            if (existingGuild == args.TargetGuild)
                            {
                                if (args.TargetGuild.MemberIds.Contains(kickedMember.Id) || (args.TargetGuild.MateIds.Contains(kickedMember.Id) && args.TargetGuild.CaptainId == context.User.Id))
                                {
                                    foundGuild = true;
                                    args.kickedMembers.Add(kickedMember);
                                }
                            }
                        }

                        if (!foundGuild)
                        {
                            args.parseErrors.Add($"User {kickedMember.Mention} is not in a guild you manage!");
                        }

                    }
                }
                else
                {
                    args.parseErrors.Add($"Unable to parse `{context.Arguments.First}` to a guild user!");
                }
            }
            if (args.kickedMembers.Count == 0)
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[0], "Could not parse any of your arguments to members!\n" + string.Join('\n', args.parseErrors)));
            }
            else
            {
                return Task.FromResult(new ArgumentParseResult(args));
            }
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            StringBuilder kicked = new StringBuilder();
            for (int i = 0; i < args.kickedMembers.Count; i++)
            {
                SocketGuildUser kickedMember = args.kickedMembers[i];
                await MinecraftGuildModel.MemberLeaveGuildAsync(args.TargetGuild, kickedMember);

                kicked.Append(kickedMember.Mention);
                if (i < args.kickedMembers.Count - 1)
                {
                    kicked.Append(", ");
                }
            }

            if (args.parseErrors.Count > 0)
            {
                await context.Channel.SendEmbedAsync($"Kicked members: {kicked}\n\nFailed to parse some of the members:\n{string.Join('\n', args.parseErrors)}");
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
        public override HandledContexts ArgumentParserMethod => HandledContexts.None;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;
        public override string Summary => SUMMARY;
        public override string Remarks => REMARKS;
        public override string Link => LINK;
        public override Argument[] Arguments => ARGS;
        public const string SUMMARY = "Leave a guild";
        public const string REMARKS = "A captain can only leave their guild if no members are left, deleting it in the progress";
        public const string LINK = "https://docs.google.com/document/d/1IdTQoq2l9YhF5Tlj5lBYz5Zcz56NQEgL3Hg5Dg2RyWs/edit#heading=h.94kph266h8s2";
        public static readonly Argument[] ARGS = new Argument[] { };
        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(Var.MinecraftBranchRoleBotVarId), new MinecraftGuildRankPrecondition(GuildRank.Regular) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;

        public LeaveGuildCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
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
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;
        public override string Summary => SUMMARY;
        public override string Remarks => REMARKS;
        public override string Link => LINK;
        public override Argument[] Arguments => ARGS;
        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(Var.MinecraftBranchRoleBotVarId), new MinecraftGuildRankPrecondition(GuildRank.Captain) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;
        public const string SUMMARY = "Pass your captain position to another member";
        public const string REMARKS = default;
        public const string LINK = "https://docs.google.com/document/d/1IdTQoq2l9YhF5Tlj5lBYz5Zcz56NQEgL3Hg5Dg2RyWs/edit#heading=h.z9qkxx7wamoo";
        public static readonly Argument[] ARGS = new Argument[] { new Argument("Member", "The member you want to pass your captain position to") };

        public class ArgumentContainer
        {
            public MinecraftGuild TargetGuild;
            public SocketGuildUser NewCaptain;
        }

        public PassCaptainRightsCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer args = new ArgumentContainer();

            bool ownsGuild = false;
            if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out args.TargetGuild))
            {
                ownsGuild = args.TargetGuild.CaptainId == context.User.Id;
            }

            if (!ownsGuild)
            {
                return Task.FromResult(new ArgumentParseResult("You are not a captain of a guild!"));
            }

            if (!args.TargetGuild.Active)
            {
                return Task.FromResult(new ArgumentParseResult("Your guild is set to inactive! Contact an admin to reactivate it"));
            }

            if (ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out args.NewCaptain))
            {
                if (args.NewCaptain.Id == context.User.Id)
                {
                    return Task.FromResult(new ArgumentParseResult(ARGS[0], "Can not pass captain rights to yourself!"));
                }
                else if (!args.TargetGuild.MemberIds.Contains(args.NewCaptain.Id) && !args.TargetGuild.MateIds.Contains(args.NewCaptain.Id))
                {
                    return Task.FromResult(new ArgumentParseResult(ARGS[0], "Can not pass captain rights to a user not in your guild!"));
                }
            }
            else
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[0], "Could not parse to a valid user!"));
            }

            return Task.FromResult(new ArgumentParseResult(args));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            if (await MinecraftGuildModel.SetGuildCaptain(args.TargetGuild, args.NewCaptain, context.GuildUser))
            {
                await context.Channel.SendEmbedAsync($"{context.GuildUser.Mention} passed on captain rights to {args.NewCaptain.Mention}!");
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
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;
        public override string Summary => SUMMARY;
        public override string Remarks => REMARKS;
        public override string Link => LINK;
        public override Argument[] Arguments => ARGS;
        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(Var.MinecraftBranchRoleBotVarId), new MinecraftGuildRankPrecondition(GuildRank.Captain) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;
        public const string SUMMARY = "Promote a member of your guild to mate";
        public const string REMARKS = "Mates are able to invite and kick members of your guild";
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] { new Argument("Mate", "The member of your guild you want to promote to mate rank") };

        public PromoteMateCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer {
            public MinecraftGuild TargetGuild;
            public SocketGuildUser NewMate;
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer args = new ArgumentContainer();

            bool ownsGuild = false;
            if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out args.TargetGuild))
            {
                ownsGuild = args.TargetGuild.CaptainId == context.User.Id;
            }

            if (!ownsGuild)
            {
                return Task.FromResult(new ArgumentParseResult("You are not a captain of a guild!"));
            }

            if (!args.TargetGuild.Active)
            {
                return Task.FromResult(new ArgumentParseResult("Your guild is set to inactive! Contact an admin to reactivate it"));
            }

            if (ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out args.NewMate, allowSelf: false))
            {
                if (MinecraftGuildModel.TryGetGuildOfUser(args.NewMate.Id, out MinecraftGuild MateGuild))
                {
                    if (MateGuild == args.TargetGuild)
                    {
                        if (!MateGuild.MemberIds.Contains(args.NewMate.Id))
                        {
                            return Task.FromResult(new ArgumentParseResult(ARGS[0], "The user promoted to mate must be a regular guild member!"));
                        }
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(ARGS[0], "The user you want to promote is not your guild"));
                    }
                }
                else
                {
                    return Task.FromResult(new ArgumentParseResult(ARGS[0], "The user you want to promote is not your guild"));
                }
            }
            else
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[0], $"Could not parse {context.Arguments.First} to a user in this guild!"));
            }

            return Task.FromResult(new ArgumentParseResult(args));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            if (await MinecraftGuildModel.PromoteGuildMember(args.TargetGuild, args.NewMate))
            {
                await context.Channel.SendEmbedAsync($"Successfully promoted {args.NewMate.Mention} to `Mate` rank!");
            }
            else
            {
                await context.Channel.SendEmbedAsync($"Failed to promote {args.NewMate.Mention} to `Mate` rank!", true);
            }
        }
    }

    #endregion
    #region demote

    class DemoteMateCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;
        public override string Summary => SUMMARY;
        public override string Remarks => REMARKS;
        public override string Link => LINK;
        public override Argument[] Arguments => ARGS;
        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(Var.MinecraftBranchRoleBotVarId), new MinecraftGuildRankPrecondition(GuildRank.Captain) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;
        public const string SUMMARY = "Demote a mate in your guild to a regular member";
        public const string REMARKS = default;
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] { new Argument("Mate", "The mate in your guild you want to demote to a regular member") };

        public DemoteMateCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public MinecraftGuild TargetGuild;
            public SocketGuildUser DemotedMate;
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer args = new ArgumentContainer();

            bool ownsGuild = false;
            if (MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out args.TargetGuild))
            {
                ownsGuild = args.TargetGuild.CaptainId == context.User.Id;
            }

            if (!ownsGuild)
            {
                return Task.FromResult(new ArgumentParseResult("You are not a captain of a guild!"));
            }

            if (!args.TargetGuild.Active)
            {
                return Task.FromResult(new ArgumentParseResult("Your guild is set to inactive! Contact an admin to reactivate it"));
            }

            if (ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out args.DemotedMate, allowSelf: false))
            {
                if (MinecraftGuildModel.TryGetGuildOfUser(args.DemotedMate.Id, out MinecraftGuild MateGuild))
                {
                    if (MateGuild == args.TargetGuild)
                    {
                        if (!MateGuild.MateIds.Contains(args.DemotedMate.Id))
                        {
                            return Task.FromResult(new ArgumentParseResult(ARGS[0], "The user demoted to regular member must be a mate!"));
                        }
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(ARGS[0], "The user you want to demote is not your guild"));
                    }
                }
                else
                {
                    return Task.FromResult(new ArgumentParseResult(ARGS[0], "The user you want to demote is not your guild"));
                }
            }
            else
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[0], $"Could not parse {context.Arguments.First} to a user in this guild!"));
            }

            return Task.FromResult(new ArgumentParseResult(args));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            if (await MinecraftGuildModel.DemoteGuildMember(args.TargetGuild, args.DemotedMate))
            {
                await context.Channel.SendEmbedAsync($"Successfully demote {args.DemotedMate.Mention} to `Regular Member` rank!");
            }
            else
            {
                await context.Channel.SendEmbedAsync($"Failed to demote {args.DemotedMate.Mention} to `Mate` rank!", true);
            }
        }
    }

    #endregion
    #region sync

    class SyncGuildsCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;
        public override string Summary => SUMMARY;
        public override string Remarks => REMARKS;
        public override string Link => LINK;
        public override Argument[] Arguments => ARGS;
        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(BotCore.ADMINROLE_BOTVARID) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;

        public const string SUMMARY = "Start the guild info syncing process";
        public const string REMARKS = "This command is used to sync data internally stored with discord data";
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] { new Argument("GuildId", "Discord Id of the guild (Discord Server) you want to sync against", true) };

        public SyncGuildsCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            SocketGuild guild;

            if (context.Arguments.Count == 0)
            {
                guild = context.Guild;
            }
            else
            {
                if (ulong.TryParse(context.Arguments.First, out ulong guildId))
                {
                    guild = BotCore.Client.GetGuild(guildId);
                    if (guild == null)
                    {
                        return Task.FromResult(new ArgumentParseResult(ARGS[0], $"Could not find a guild with id `{guildId}`"));
                    }
                }
                else
                {
                    return Task.FromResult(new ArgumentParseResult(ARGS[0], $"Could not parse {context.Arguments.First} to a valid discord Id"));
                }
            }

            return Task.FromResult(new ArgumentParseResult(guild));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            SocketGuild guild = argObj as SocketGuild;

            List<DesyncItem> problems = new List<DesyncItem>();

            List<ulong> userIds = new List<ulong>(guild.Users.Select(user => { return user.Id; }));
            List<Tuple<SocketRole, MinecraftGuild>> roles = new List<Tuple<SocketRole, MinecraftGuild>>();

            // Missing Guild Channels and Roles

            foreach (MinecraftGuild minecraftGuild in MinecraftGuildModel.Guilds)
            {
                minecraftGuild.TryFindNameAndColor();
                bool channelFound = guild.GetTextChannel(minecraftGuild.ChannelId) != null;
                SocketRole guildRole = guild.GetRole(minecraftGuild.RoleId);
                bool roleFound = guildRole != null;

                if (!channelFound)
                {
                    // Channel missing
                    problems.Add(new DesyncItem("Channel Not Found", $"The channel of guild \"{minecraftGuild.Name}\" couldn't be located. Suggested action: Assign new channel with `/guild modify {minecraftGuild.Name_CommandSafe} setchannel:<NewChannel>`", new DeleteGuildDatasetOption(minecraftGuild)));
                }
                if (!roleFound)
                {
                    // role missing
                    problems.Add(new DesyncItem("Role Not Found", $"The role of guild \"{minecraftGuild.Name}\" couldn't be located (guild names can not be loaded without roles!). Suggested action: Assign new role with `/guild modify {minecraftGuild.Name_CommandSafe} setrole:<NewRole>`", new DeleteGuildDatasetOption(minecraftGuild)));
                }
                else
                {
                    roles.Add(new Tuple<SocketRole, MinecraftGuild>(guildRole, minecraftGuild));
                }
            }

            // Missing Users

            foreach (MinecraftGuild minecraftGuild in MinecraftGuildModel.Guilds)
            {
                SocketRole guildRole = roles.Find((Tuple<SocketRole, MinecraftGuild> tuple) => { return tuple.Item2 == minecraftGuild; })?.Item1;

                if (!userIds.Contains(minecraftGuild.CaptainId))
                {
                    // Captain Missing!
                    problems.Add(new DesyncItem("Captain Not Found", $"Captain (ID: `{minecraftGuild.CaptainId}`, DebugMention: {Markdown.Mention_User(minecraftGuild.CaptainId)}) of Guild \"{minecraftGuild.Name}\" missing! Recommended action: Reassign new captain with command `/guild modify {minecraftGuild.Name_CommandSafe} setcaptain:<NewCaptain>`"));
                }
                else if ((guildRole != null) && !guildRole.Members.Any((SocketGuildUser user) => { return user.Id == minecraftGuild.CaptainId; }))
                {
                    // Captain does not have guild role
                    problems.Add(new DesyncItem("Captain without guild role", $"Captain {Markdown.Mention_User(minecraftGuild.CaptainId)} of Guild \"{minecraftGuild.Name}\" does not have a guild role!", new AddRoleOption(guild.GetUser(minecraftGuild.CaptainId), guildRole), new DeleteGuildDatasetOption(minecraftGuild)));
                }
                foreach (ulong mateId in minecraftGuild.MateIds)
                {
                    if (!userIds.Contains(mateId))
                    {
                        // Mate Missing!
                        problems.Add(new DesyncItem("Mate Member Not Found", $"Mate (ID: `{mateId}`, DebugMention: {Markdown.Mention_User(mateId)}) of Guild \"{minecraftGuild.Name}\" missing!",
                            new RemoveMemberDatasetDesyncOption(minecraftGuild, mateId)));
                    }
                    else if ((guildRole != null) && !guildRole.Members.Any((SocketGuildUser user) => { return user.Id == mateId; }))
                    {
                        // Mate does not have guild role
                        problems.Add(new DesyncItem("Mate without guild role", $"Mate {Markdown.Mention_User(mateId)} of Guild \"{minecraftGuild.Name}\" does not have a guild role!", new AddRoleOption(guild.GetUser(mateId), guildRole), new RemoveMemberDatasetDesyncOption(minecraftGuild, mateId)));
                    }
                }
                foreach (ulong memberId in minecraftGuild.MemberIds)
                {
                    if (!userIds.Contains(memberId))
                    {
                        // Member Missing!
                        problems.Add(new DesyncItem("Member Not Found", $"Member (ID: `{memberId}`, DebugMention: {Markdown.Mention_User(memberId)}) of Guild \"{minecraftGuild.Name}\" missing!",
                            new RemoveMemberDatasetDesyncOption(minecraftGuild, memberId)));
                    }
                    else if ((guildRole != null) && !guildRole.Members.Any((SocketGuildUser user) => { return user.Id == memberId; }))
                    {
                        problems.Add(new DesyncItem("Mate without guild role", $"Mate {Markdown.Mention_User(memberId)} of Guild \"{minecraftGuild.Name}\" does not have a guild role!", new AddRoleOption(guild.GetUser(memberId), guildRole), new RemoveMemberDatasetDesyncOption(minecraftGuild, memberId)));
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
