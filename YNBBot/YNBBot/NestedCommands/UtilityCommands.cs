﻿

using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YNBBot.NestedCommands
{
    class UserInfoCommand : Command
    {
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicAsync;

        private SocketUser User;

        public UserInfoCommand(string identifier) : base(identifier)
        {
            List<CommandArgument> arguments = new List<CommandArgument>();
            arguments.Add(new CommandArgument("User", ArgumentParsingHelper.GENERIC_PARSED_USER, true));
            InitializeHelp("Provides a collection of info for a given user", arguments.ToArray());
        }

        protected override async Task<ArgumentParseResult> TryParseArgumentsAsync(CommandContext context)
        {
            if (context.Args.Count == 0)
            {
                User = context.User;
            }
            else
            {
                if (GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext))
                {
                    if (ArgumentParsingHelper.TryParseGuildUser(guildContext, context.Args.First, out SocketGuildUser guildUser))
                    {
                        User = guildUser;
                    }
                }
                else
                {
                    User = await ArgumentParsingHelper.ParseUser(context, context.Args.First);
                }
            }

            if (User != null)
            {
                return ArgumentParseResult.SuccessfullParse;
            }
            else
            {
                return new ArgumentParseResult(Arguments[0], "Failed to parse to a User!");
            }
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = Var.BOTCOLOR,
                Title = "UserInfo"
            };
            embed.AddField("Command Access Level", Var.client.GetAccessLevel(User.Id), true);
            embed.AddField("Discriminator", string.Format("{0}#{1}", User.Username, User.Discriminator), true);
            embed.AddField("Mention", '\\' + User.Mention, true);
            embed.AddField("Discord Snowflake Id", User.Id, true);
            SocketGuildUser guildUser = User as SocketGuildUser;
            if (guildUser != null)
            {
                if (!string.IsNullOrEmpty(guildUser.Nickname))
                {
                    embed.AddField("Nickname", guildUser.Nickname, true);
                    embed.Author = new EmbedAuthorBuilder()
                    {
                        Name = guildUser.Nickname,
                        IconUrl = User.GetAvatarUrl()
                    };
                }
                if (guildUser.JoinedAt != null)
                {
                    embed.AddField("Joined Guild", guildUser.JoinedAt?.ToString("r"), true);
                }
            }

            if (embed.Author == null)
            {
                embed.Author = new EmbedAuthorBuilder()
                {
                    Name = User.Username,
                    IconUrl = User.GetAvatarUrl()
                };
            }

            embed.AddField("Joined Discord", User.CreatedAt.ToString("r"), true);

            embed.ImageUrl = User.GetAvatarUrl();
            await context.Channel.SendEmbedAsync(embed);
        }
    }

    class AvatarCommand : Command
    {
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.BasicAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.BasicAsync;

        private SocketUser User;

        public AvatarCommand(string identifier) : base(identifier)
        {
            List<CommandArgument> arguments = new List<CommandArgument>();
            arguments.Add(new CommandArgument("User", ArgumentParsingHelper.GENERIC_PARSED_USER, true));
            InitializeHelp("Provides a users profile picture", arguments.ToArray());
        }

        protected override async Task<ArgumentParseResult> TryParseArgumentsAsync(CommandContext context)
        {
            if (context.Args.Count == 0)
            {
                User = context.User;
            }
            else
            {
                if (GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext))
                {
                    if (ArgumentParsingHelper.TryParseGuildUser(guildContext, context.Args.First, out SocketGuildUser guildUser))
                    {
                        User = guildUser;
                    }
                }
                else
                {
                    User = await ArgumentParsingHelper.ParseUser(context, context.Args.First);
                }
            }

            if (User != null)
            {
                return ArgumentParseResult.SuccessfullParse;
            }
            else
            {
                return new ArgumentParseResult(Arguments[0], "Failed to parse to a User!");
            }
        }

        protected override async Task HandleCommandAsync(CommandContext context)
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = Var.BOTCOLOR,
            };
            SocketGuildUser guildUser = User as SocketGuildUser;
            if ((guildUser != null) && !string.IsNullOrEmpty(guildUser.Nickname))
            {
                embed.Author = new EmbedAuthorBuilder()
                {
                    Name = guildUser.Nickname,
                    IconUrl = User.GetAvatarUrl()
                };
            }
            else
            {
                embed.Author = new EmbedAuthorBuilder()
                {
                    Name = User.Username,
                    IconUrl = User.GetAvatarUrl()
                };
            }
            embed.ImageUrl = User.GetAvatarUrl(size: 2048);
            await context.Channel.SendEmbedAsync(embed);
        }
    }
}