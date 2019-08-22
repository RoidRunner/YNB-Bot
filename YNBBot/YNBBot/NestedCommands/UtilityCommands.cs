

using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using YNBBot.MinecraftGuildSystem;

namespace YNBBot.NestedCommands
{
    #region userinfo

    class UserInfoCommand : Command
    {
        public const string SUMMARY = "Provides a collection of info for a given user";
        public const string REMARKS = default;
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] { new Argument("User", ArgumentParsing.GENERIC_PARSED_USER, true) };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { };

        private SocketUser User;

        public UserInfoCommand(string identifier) : base(identifier, OverriddenMethod.BasicAsync, OverriddenMethod.BasicAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
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
                    if (ArgumentParsing.TryParseGuildUser(guildContext, context.Args.First, out SocketGuildUser guildUser))
                    {
                        User = guildUser;
                    }
                }
                else
                {
                    User = await ArgumentParsing.ParseUser(context, context.Args.First);
                }
            }

            if (User != null)
            {
                return ArgumentParseResult.SuccessfullParse;
            }
            else
            {
                return new ArgumentParseResult(ARGS[0], "Failed to parse to a User!");
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
            if (MinecraftGuildModel.TryGetGuildOfUser(User.Id, out MinecraftGuild minecraftGuild))
            {
                embed.AddField("Minecraft Guild Membership", $"\"{minecraftGuild.Name}\", Rank `{minecraftGuild.GetMemberRank(User.Id)}`");
            }
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
                    embed.AddField("Joined "+ guildUser.Guild.Name, guildUser.JoinedAt?.ToString("r"), true);
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

    #endregion
    #region avatar

    class AvatarCommand : Command
    {
        public const string SUMMARY = "Provides a users profile picture";
        public const string REMARKS = default;
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] { new Argument("User", ArgumentParsing.GENERIC_PARSED_USER, true) };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { };

        private SocketUser User;

        public AvatarCommand(string identifier) : base(identifier, OverriddenMethod.BasicAsync, OverriddenMethod.BasicAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
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
                    if (ArgumentParsing.TryParseGuildUser(guildContext, context.Args.First, out SocketGuildUser guildUser))
                    {
                        User = guildUser;
                    }
                }
                else
                {
                    User = await ArgumentParsing.ParseUser(context, context.Args.First);
                }
            }

            if (User != null)
            {
                return ArgumentParseResult.SuccessfullParse;
            }
            else
            {
                return new ArgumentParseResult(ARGS[0], "Failed to parse to a User!");
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

    #endregion
    #region about

    class AboutCommand : Command
    {
        public const string SUMMARY = "Lists information about the bot";
        public const string REMARKS = default;
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] { };
        public static readonly Precondition[] AUTHCHECKS = new Precondition[] { };

        public AboutCommand(string identifier) : base(identifier, OverriddenMethod.None, OverriddenMethod.BasicAsync, false, ARGS, AUTHCHECKS, SUMMARY, REMARKS, LINK)
        {
        }

        private static readonly EmbedBuilder AboutEmbed;

        static AboutCommand()
        {
            AboutEmbed = new EmbedBuilder()
            {
                Color = Var.BOTCOLOR,
                Title = "You Need Bot"
            };
            AboutEmbed.AddField("Version", "v" + Var.VERSION.ToString());
            AboutEmbed.AddField("Credits", "Programming: <@117260771200598019>");
        }

        protected override Task HandleCommandAsync(CommandContext context)
        {
            if (string.IsNullOrEmpty(AboutEmbed.ThumbnailUrl))
            {
                AboutEmbed.ThumbnailUrl = Var.client.CurrentUser.GetAvatarUrl();
            }
            return context.Channel.SendEmbedAsync(AboutEmbed);
        }
    }

    #endregion
}