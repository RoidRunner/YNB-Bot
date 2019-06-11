using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using YNBBot.Interactive;
using YNBBot.MinecraftGuildSystem;

namespace YNBBot.NestedCommands
{

    #region found
    class CreateGuildCommand : Command
    {
        public override string Identifier => "found";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.GuildSynchronous;

        public CreateGuildCommand()
        {
            RequireAccessLevel = AccessLevel.Minecraft;

            CommandArgument[] arguments = new CommandArgument[3];
            arguments[0] = new CommandArgument("Name", "The name of the guild. Will be the name of the channel and role created. Also applies to ingame naming");
            arguments[1] = new CommandArgument("Color", $"The color of the guild. Will be the color of the role created. Also applies to ingame color. Available are `{string.Join(", ", MinecraftGuildSystem.MinecraftGuildModel.AvailableColors)}`");
            arguments[2] = new CommandArgument("Members", "Minimum of two members, selected either by discord snowflake id or mention", multiple: true);
            InitializeHelp("Requests creation of a new minecraft guild", arguments, $"{arguments[0]} and {arguments[1]} have to be free to take, all invited members ({arguments[2]}) have to accept the invitation, and an admin has to confirm the creation of the new guild");
        }

        string GuildName;
        GuildColor GuildColor;
        List<SocketGuildUser> Members = new List<SocketGuildUser>();

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (MinecraftGuildSystem.MinecraftGuildModel.TryGetGuildOfUser(context.User.Id, out MinecraftGuild contextUserGuild))
            {
                return new ArgumentParseResult($"You can not found a new guild because you are still part of `{contextUserGuild.Name}`");
            }

            GuildName = context.Args[0];

            context.Args.Index++;

            if (GuildName.StartsWith('\"'))
            {
                for (; context.Args.Index < context.Args.TotalCount; context.Args.Index++)
                {
                    GuildName += " " + context.Args.First;
                    if (context.Args.First.EndsWith('\"'))
                    {
                        GuildName = GuildName.Trim('\"');
                        break;
                    }
                }

                context.Args.Index++;
            }

            if (GuildName.Length < 5)
            {
                return new ArgumentParseResult(Arguments[0], "Too short! Minimum of 5 Characters");
            }
            if (GuildName.Length > 25)
            {
                return new ArgumentParseResult(Arguments[0], "Too long! Maximum of 25 Characters");
            }
            if (!MinecraftGuildSystem.MinecraftGuildModel.NameIsAvailable(GuildName))
            {
                return new ArgumentParseResult(Arguments[0], "A guild with this name already exists!");
            }

            if (!Enum.TryParse(context.Args[0], out GuildColor))
            {
                return new ArgumentParseResult(Arguments[1], $"Could not parse to an available guild color! Available are `{string.Join(", ", MinecraftGuildModel.AvailableColors)}`");
            }
            if (!MinecraftGuildModel.ColorIsAvailable(GuildColor))
            {
                return new ArgumentParseResult(Arguments[1], $"A guild with this color already exists! Available are `{string.Join(", ", MinecraftGuildModel.AvailableColors)}`");
            }

            context.Args.Index++;

            Members.Clear();
            if (context.Args.Count < MinecraftGuildModel.MIN_GUILDFOUNDINGMEMBERS)
            {
                return new ArgumentParseResult(Arguments[2], $"You need to supply a minimum of {MinecraftGuildModel.MIN_GUILDFOUNDINGMEMBERS} members!");
            }

            for (int i = 0; i < context.Args.Count; i++)
            {
                if (ArgumentParsingHelper.TryParseGuildUser(context, context.Args[i], out SocketGuildUser member, allowSelf: false))
                {
                    if (member.Id == context.User.Id)
                    {
                        return new ArgumentParseResult(Arguments[2], "Can not add yourself as a guild member!");
                    }
                    if (MinecraftGuildSystem.MinecraftGuildModel.TryGetGuildOfUser(member.Id, out MinecraftGuild memberGuild))
                    {
                        return new ArgumentParseResult(Arguments[2], $"Can not invite {member.Mention}, because he is already part of {memberGuild.Name}");
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
                        return new ArgumentParseResult(Arguments[2], $"Can not invite {member.Mention}, because he is not part of the minecraft branch!");
                    }

                    Members.Add(member);
                }
                else
                {
                    return new ArgumentParseResult(Arguments[2], $"Could not parse `{context.Args[i]}` to a guild user!");
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
        public override string Identifier => "modify";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicSynchronous;

        public ModifyGuildCommand()
        {
            RequireAccessLevel = AccessLevel.Admin;
            CommandArgument[] arguments = new CommandArgument[]
            {
                new CommandArgument("Name", "Name of the guild to delete"),
                new CommandArgument("Actions", $"The modifying action you want to take", multiple:true)
            };
            InitializeHelp("Modifies guild attributes", arguments, "For a list of modifying actions see below. Some actions require an argument to be passed after them following this syntax: `<Action>:<Argument>`. Multiword arguments are to be encased with quotation marks '\"'.\n\n" +
                $"`{GuildModifyActions.delete}` - Removes the guild dataset, channel and role\n" +
                $"`{GuildModifyActions.deletedataset}` - Removes the guild dataset\n" +
                $"`{GuildModifyActions.setchannel}:<Channel>` - Sets the guild channel\n" +
                $"`{GuildModifyActions.setrole}:<Role>` - Sets the guild role\n" +
                $"`{GuildModifyActions.rename}:<Name>` - Renames the guild\n" +
                $"`{GuildModifyActions.recolor}:<Color>` - Assignes a new color to the guild\n" +
                $"`{GuildModifyActions.setcaptain}:<Captain>` - Sets the captain of the guild. Has to be a member!\n" +
                $"`{GuildModifyActions.addmember}:<Member>` - Manually adds a member to the guild\n" +
                $"`{GuildModifyActions.removemember}:<Member>` - Manually removes a member from the guild"
                //$"`{GuildModifyActions}` : " + 
                );
        }

        private enum GuildModifyActions
        {
            delete,
            deletedataset,
            rename,
            recolor,
            setchannel,
            setrole,
            setcaptain,
            addmember,
            removemember,
            timestamp
        }

        private readonly bool[] ActionRequiresArg = new bool[] { false, false, true, true, true, true, true, true, true, true };

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
            string guildName = context.Args[0];

            context.Args.Index++;

            if (guildName.StartsWith('\"'))
            {
                for (; context.Args.Index < context.Args.TotalCount; context.Args.Index++)
                {
                    guildName += " " + context.Args.First;
                    if (context.Args.First.EndsWith('\"'))
                    {
                        guildName = guildName.Trim('\"');
                        break;
                    }
                }

                context.Args.Index++;
            }

            if (!MinecraftGuildModel.GetGuild(guildName, out TargetGuild, true))
            {
                return new ArgumentParseResult(Arguments[0], "Unable to find a guild of this name!");
            }

            Actions.Clear();

            GuildModifyActions action = GuildModifyActions.delete;
            string arg = string.Empty;
            ModifyActionsParseStep parseStep = ModifyActionsParseStep.IdentifyAction;
            for (; context.Args.Count > 0; context.Args.Index++)
            {
                string current = context.Args.First;
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
                                    return new ArgumentParseResult(Arguments[1], $"Unable to parse `{splits[0]}` to a guild modify action!");
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
                                return new ArgumentParseResult(Arguments[1], $"Unable to parse `{current}` to a guild modify action!");
                            }
                        }
                        else if (Enum.TryParse(current, out action))
                        {
                            if (requireArgument(action))
                            {
                                return new ArgumentParseResult(Arguments[1], $"Action `{action}` requires an argument!");
                            }
                            else
                            {
                                Actions.Add(new GuildAction(action, null));
                            }
                        }
                        else
                        {
                            return new ArgumentParseResult(Arguments[1], $"Unable to parse `{current}` to a guild modify action!");
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
                return new ArgumentParseResult(Arguments[1], $"Argument {arg} was not terminated with quotation marks '\"'");
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
                        await MinecraftGuildModel.DeleteGuildAsync(TargetGuild);
                        i = Actions.Count;
                        successful.Add(action);
                        break;
                    case GuildModifyActions.deletedataset:
                        await MinecraftGuildModel.DeleteGuildDatasetAsync(TargetGuild);
                        i = Actions.Count;
                        break;
                    case GuildModifyActions.setchannel:
                        if (ArgumentParsingHelper.TryParseGuildTextChannel(context, action.Argument, out SocketTextChannel newGuildChannel))
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
                            if (ArgumentParsingHelper.TryParseRole(guildContext, action.Argument, out SocketRole newGuildRole))
                            {
                                await newGuildRole.ModifyAsync(RoleProperties =>
                                {
                                    RoleProperties.Name = TargetGuild.Name;
                                    RoleProperties.Color = new Color((uint)TargetGuild.Color);
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
                            TargetGuild.Name = action.Argument;
                            if (Var.client.TryGetRole(TargetGuild.RoleId, out SocketRole guildRole))
                            {
                                await guildRole.ModifyAsync(RoleProperties =>
                                {
                                    RoleProperties.Name = TargetGuild.Name;
                                });
                            }
                            if (GuildChannelHelper.TryGetChannel(TargetGuild.ChannelId, out SocketTextChannel guildChannel))
                            {
                                await guildChannel.ModifyAsync(GuildChannelProperties =>
                                {
                                    GuildChannelProperties.Name = TargetGuild.Name;
                                });
                            }
                            successful.Add(action);
                            saveChanges = true;
                        }
                        break;
                    case GuildModifyActions.recolor:
                        if (Enum.TryParse(action.Argument, out GuildColor newColor))
                        {
                            TargetGuild.Color = newColor;
                            if (Var.client.TryGetRole(TargetGuild.RoleId, out SocketRole guildRole))
                            {
                                await guildRole.ModifyAsync(RoleProperties =>
                                {
                                    RoleProperties.Color = new Color((uint)TargetGuild.Color);
                                });
                            }
                            saveChanges = true;
                            successful.Add(action);
                        }
                        else
                        {
                            errors.Add($"`{action}` - Unable to parse {action.Argument} to a minecraft guild color!");
                        }
                        break;
                    case GuildModifyActions.setcaptain:
                        if (hasGuildContext)
                        {
                            if (ArgumentParsingHelper.TryParseGuildUser(guildContext, action.Argument, out SocketGuildUser newCaptain))
                            {
                                if (TargetGuild.CaptainId == newCaptain.Id)
                                {
                                    errors.Add($"`{action}` - The new captain is already captain of this guild!3");
                                }
                                else if (!TargetGuild.MemberIds.Contains(newCaptain.Id))
                                {
                                    errors.Add($"`{action}` - The new captain has to be a member of this guild!");
                                }
                                else
                                {
                                    TargetGuild.MemberIds.Add(TargetGuild.CaptainId);
                                    TargetGuild.MemberIds.Remove(newCaptain.Id);
                                    TargetGuild.CaptainId = newCaptain.Id;
                                    saveChanges = true;
                                    successful.Add(action);
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
                            errors.Add($"`{action}` - ");
                        }
                        break;
                    case GuildModifyActions.addmember:
                        if (hasGuildContext)
                        {
                            if (ArgumentParsingHelper.TryParseGuildUser(guildContext, action.Argument, out SocketGuildUser newMember))
                            {
                                if (TargetGuild.MemberIds.Contains(newMember.Id))
                                {
                                    errors.Add($"`{action}` - This user is already member of this guild!");
                                }
                                else
                                {
                                    TargetGuild.MemberIds.Add(newMember.Id);
                                    SocketRole guildRole = guildContext.Guild.GetRole(TargetGuild.RoleId);
                                    bool hasRole = false;
                                    foreach (SocketRole role in newMember.Roles)
                                    {
                                        if (role.Id == TargetGuild.RoleId)
                                        {
                                            hasRole = true;
                                            break;
                                        }
                                    }
                                    if (guildRole != null && !hasRole)
                                    {
                                        await newMember.AddRoleAsync(guildRole);
                                    }
                                    saveChanges = true;
                                    successful.Add(action);
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
                            errors.Add($"`{action}` - ");
                        }
                        break;
                    case GuildModifyActions.removemember:
                        if (hasGuildContext)
                        {
                            if (ArgumentParsingHelper.TryParseGuildUser(guildContext, action.Argument, out SocketGuildUser leavingMember))
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
                                    TargetGuild.MemberIds.Remove(leavingMember.Id);
                                    SocketRole guildRole = guildContext.Guild.GetRole(TargetGuild.RoleId);
                                    bool hasRole = false;
                                    foreach (SocketRole role in leavingMember.Roles)
                                    {
                                        if (role.Id == TargetGuild.RoleId)
                                        {
                                            hasRole = true;
                                            break;
                                        }
                                    }
                                    if (hasRole && guildRole != null)
                                    {
                                        await leavingMember.RemoveRoleAsync(guildRole);
                                    }
                                    saveChanges = true;
                                    successful.Add(action);
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
                    description.AppendLine(Macros.InlineCodeBlock(action));
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
                Color = (successful.Count == 0 ? Var.ERRORCOLOR : Var.BOTCOLOR),
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
        public override string Identifier => "info";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicSynchronous;

        public GuildInfoCommand()
        {
            RequireAccessLevel = AccessLevel.Minecraft;

            CommandArgument[] arguments = new CommandArgument[]
            {
                new CommandArgument("Name", "Name of the guild to get info on", true, true)
            };
            InitializeHelp("Shows public info on all or one individual guild", arguments, "If no Name is supplied, will display a list of all guilds");
        }

        private MinecraftGuild TargetGuild;

        protected override ArgumentParseResult TryParseArgumentsSynchronous(CommandContext context)
        {
            if (context.Args.Count == 0)
            {
                TargetGuild = null;
            }
            else
            {
                string guildName = context.Args[0];

                context.Args.Index++;

                if (guildName.StartsWith('\"'))
                {
                    for (; context.Args.Index < context.Args.TotalCount; context.Args.Index++)
                    {
                        guildName += " " + context.Args.First;
                        if (context.Args.First.EndsWith('\"'))
                        {
                            guildName = guildName.Trim('\"');
                            break;
                        }
                    }

                    context.Args.Index++;
                }

                if (!MinecraftGuildModel.GetGuild(guildName, out TargetGuild, true))
                {
                    return new ArgumentParseResult(Arguments[0], "Unable to find a guild of this name!");
                }
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
                    Color = new Color((uint)TargetGuild.Color),
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
                SocketUser guildCaptain = Var.client.GetUser(TargetGuild.CaptainId);
                if (guildCaptain != null)
                {
                    members.AppendLine(guildCaptain.Mention);
                }
                else
                {
                    members.AppendLine(Macros.InlineCodeBlock(TargetGuild.CaptainId));
                }
                members.AppendLine();
                members.AppendFormat("**Members - {0}**\n", TargetGuild.MemberIds.Count);
                foreach (ulong memberId in TargetGuild.MemberIds)
                {
                    SocketUser member = Var.client.GetUser(memberId);
                    if (member != null)
                    {
                        members.AppendLine(member.Mention);
                    }
                    else
                    {
                        members.AppendLine(Macros.InlineCodeBlock(memberId));
                    }
                }
                embed.AddField("Members", members);
                StringBuilder info = new StringBuilder();
                info.Append("Channel: ");
                if (GuildChannelHelper.TryGetChannel(TargetGuild.ChannelId, out SocketTextChannel channel))
                {
                    info.AppendLine(channel.Mention);
                }
                else
                {
                    info.AppendLine(Macros.InlineCodeBlock(TargetGuild.ChannelId));
                }
                info.Append("Role: ");
                if (Var.client.TryGetRole(TargetGuild.RoleId, out SocketRole role))
                {
                    info.AppendLine(role.Mention);
                }
                else
                {
                    info.AppendLine(Macros.InlineCodeBlock(TargetGuild.RoleId));
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
                    string name = "Guild Role Not Found!";
                    string color = "Guild Role Not Found!";
                    string captain;
                    if (guild.TryRetrieveNameAndColor())
                    {
                        name = $"Guild \"{guild.Name}\"";
                        color = guild.Color.ToString();
                    }
                    SocketUser guildCaptain = Var.client.GetUser(guild.CaptainId);
                    if (guildCaptain != null)
                    {
                        captain = guildCaptain.Mention;
                    }
                    else
                    {
                        captain = Macros.InlineCodeBlock(TargetGuild.CaptainId);
                    }
                    embeds.Add(Macros.EmbedField(name, $"{color}, Captain: {captain}, {guild.MemberIds.Count} Members"));
                }

                await context.Channel.SendSafeEmbedList(title, embeds);
            }

        }
    }

    #endregion
    #region invite

    class InviteMemberCommand : Command
    {
        public override string Identifier => "invite";
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.GuildAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.GuildSynchronous;

        public InviteMemberCommand()
        {
            RequireAccessLevel = AccessLevel.Minecraft;

            CommandArgument[] arguments = new CommandArgument[]
            {
                new CommandArgument("Member", "Users you want to invite to join your guild", multiple:true)
            };
            InitializeHelp("Invite members to join your guild as a guild captain", arguments, "Only users who are not already in a guild and are part of the minecraft branch can be invited");
        }

        private MinecraftGuild TargetGuild;
        private List<SocketGuildUser> newMembers = new List<SocketGuildUser>();
        private List<string> parseErrors = new List<string>();

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

            newMembers.Clear();
            parseErrors.Clear();
            for (; context.Args.Count > 0; context.Args.Index++)
            {
                if (ArgumentParsingHelper.TryParseGuildUser(context, context.Args.First, out SocketGuildUser newMember, allowSelf: false))
                {
                    if (MinecraftGuildModel.TryGetGuildOfUser(newMember.Id, out MinecraftGuild existingGuild, true))
                    {
                        parseErrors.Add($"{newMember.Mention} is already in guild \"{(existingGuild.TryRetrieveNameAndColor() ? existingGuild.Name : existingGuild.ChannelId.ToString())}\"");
                    }
                    else
                    {
                        newMembers.Add(newMember);
                    }
                }
                else
                {
                    parseErrors.Add($"Unable to parse `{context.Args.First}` to a guild user!");
                }
            }
            if (newMembers.Count == 0)
            {
                return new ArgumentParseResult(Arguments[0], "Could not parse any of your arguments to members!\n" + string.Join('\n', parseErrors));
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
                MessageInteractionDelegate onConfirm = async messageInteractionContext =>
                {
                    if (messageInteractionContext.User.Id == newMember.Id)
                    {
                        if (MinecraftGuildModel.TryGetGuildOfUser(newMember.Id, out MinecraftGuild existingGuild, true))
                        {
                            EmbedBuilder failure = new EmbedBuilder()
                            {
                                Title = "Failed",
                                Description = $"Already in guild \"{(existingGuild.TryRetrieveNameAndColor() ? existingGuild.Name : existingGuild.ChannelId.ToString())}\""
                            };
                            await messageInteractionContext.Message.ModifyAsync(MessageProperties =>
                            {
                                MessageProperties.Embed = failure.Build();
                            });
                        }
                        else
                        {
                            await MinecraftGuildModel.MemberJoinGuildAsync(TargetGuild, newMember);
                            if (GuildChannelHelper.TryGetChannel(GuildChannelHelper.AdminNotificationChannelId, out SocketTextChannel notificationsChannel))
                            {
                                await notificationsChannel.SendEmbedAsync($"{newMember} joined Guild \"{TargetGuild.Name}\"");
                            }
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                };

                await ConfirmationInteractiveMessage.CreateConfirmationMessage($"{newMember} Invitation to join Guild \"{TargetGuild.Name}\"", $"Confirm you want to join \"{TargetGuild.Name}\"", TargetGuild.DiscordColor, "", UnicodeEmoteService.Checkmark, UnicodeEmoteService.Cross, onConfirm, async x => { await InteractiveMessage.GenericInteractionEnd(x.Message, "Invitation Dismissed"); return true; });
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
}
