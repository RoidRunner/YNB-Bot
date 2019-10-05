

using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using YNBBot.MinecraftGuildSystem;
using System.Linq;
using BotCoreNET.Helpers;
using BotCoreNET;
using BotCoreNET.CommandHandling;

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
            if (context.Arguments.Count == 0)
            {
                User = context.User;
            }
            else
            {
                if (GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext))
                {
                    if (ArgumentParsing.TryParseGuildUser(guildContext, context.Arguments.First, out SocketGuildUser guildUser))
                    {
                        User = guildUser;
                    }
                }
                else
                {
                    User = await ArgumentParsing.ParseUser(context, context.Arguments.First);
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
                Color = BotCore.EmbedColor,
                Title = "UserInfo"
            };
            embed.AddField("Command Access Level", BotCore.Client.GetAccessLevel(User.Id), true);
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
            if (context.Arguments.Count == 0)
            {
                User = context.User;
            }
            else
            {
                if (GuildCommandContext.TryConvert(context, out GuildCommandContext guildContext))
                {
                    if (ArgumentParsing.TryParseGuildUser(guildContext, context.Arguments.First, out SocketGuildUser guildUser))
                    {
                        User = guildUser;
                    }
                }
                else
                {
                    User = await ArgumentParsing.ParseUser(context, context.Arguments.First);
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
                Color = BotCore.EmbedColor,
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
    #region serverinfo

    class ServerinfoCommand : Command
    {
        public const string SUMMARY = "Lists information about the current server";

        public ServerinfoCommand(string identifier) : base(identifier, OverriddenMethod.None, OverriddenMethod.GuildAsync, false, new Argument[0], null, SUMMARY, null, null)
        {

        }

        protected override Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            SocketGuild guild = context.Guild;
            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = BotCore.EmbedColor
            };
            embed.Author = new EmbedAuthorBuilder()
            {
                Name = guild.Name,
                IconUrl = guild.IconUrl
            };
            embed.AddField("Owner", guild.Owner.Mention, true);
            embed.AddField("Region", guild.VoiceRegionId, true);
            embed.AddField("Founded", guild.CreatedAt, true);
            int bots = 0;
            int online = 0;
            foreach(SocketGuildUser member in guild.Users)
            {
                if (member.IsBot || member.IsWebhook)
                {
                    bots++;
                }
                if (member.Status != UserStatus.Offline)
                {
                    online++;
                }
            }
            embed.AddField($"Members - {guild.MemberCount}", $"Online: `{online}`, Humans: `{guild.MemberCount - bots}`, Bots: `{bots}`", true);
            embed.AddField($"Channels - {guild.Channels.Count}", $"Categories: `{guild.CategoryChannels.Count}`, Text: `{guild.TextChannels.Count}`, Voice: `{guild.VoiceChannels.Count}`", true);
            List<SocketRole> roles = new List<SocketRole>(guild.Roles);
            roles.Sort(new RoleSorter());
            embed.AddField($"Roles - {guild.Roles.Count}", Macros.MaxLength(roles.OperationJoin(", ", role => { return role.Mention; }), EmbedHelper.EMBEDFIELDVALUE_MAX));

            return context.Channel.SendEmbedAsync(embed);
        }
    }

    class RoleSorter : Comparer<SocketRole>
    {
        public override int Compare(SocketRole x, SocketRole y)
        {
            return x.Position - y.Position;
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
                Color = BotCore.EmbedColor,
                Title = "You Need Bot"
            };
            AboutEmbed.AddField("Version", "v" + Var.VERSION.ToString());
            AboutEmbed.AddField("Credits", "Programming: <@117260771200598019>");
        }

        protected override Task HandleCommandAsync(CommandContext context)
        {
            if (string.IsNullOrEmpty(AboutEmbed.ThumbnailUrl))
            {
                AboutEmbed.ThumbnailUrl = BotCore.Client.CurrentUser.GetAvatarUrl();
            }
            return context.Channel.SendEmbedAsync(AboutEmbed);
        }
    }

    #endregion
}