using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YNBBot.Interactive;
using YNBBot.MinecraftGuildSystem;

namespace YNBBot.NestedCommands
{
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
}
