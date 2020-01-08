

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
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;

        public override string Summary => "Provides a collection of info for a given user";
        public override Argument[] Arguments => new Argument[] { new Argument("User", ArgumentParsing.GENERIC_PARSED_USER, true) };


        public UserInfoCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }


        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            SocketUser User;
            if (context.Arguments.Count == 0)
            {
                User = context.User;
            }
            else
            {
                if (ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out SocketGuildUser guildUser))
                {
                    User = guildUser;
                }
                else
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[0], "Failed to parse to a User!"));
                }
            }

            return Task.FromResult(new ArgumentParseResult(User));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            SocketGuildUser User = argObj as SocketGuildUser;

            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = BotCore.EmbedColor,
                Title = "UserInfo"
            };
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
                    embed.AddField("Joined " + guildUser.Guild.Name, guildUser.JoinedAt?.ToString("r"), true);
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
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;

        public override string Summary => "Provides a users profile picture";
        public override Argument[] Arguments => new Argument[] { new Argument("User", ArgumentParsing.GENERIC_PARSED_USER, true) };

        public AvatarCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            SocketUser User;
            if (context.Arguments.Count == 0)
            {
                User = context.User;
            }
            else
            {
                if (ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out SocketGuildUser guildUser))
                {
                    User = guildUser;
                }
                else
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[0], "Failed to parse to a User!"));
                }
            }

            return Task.FromResult(new ArgumentParseResult(User));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            SocketGuildUser User = argObj as SocketGuildUser;

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
        public override HandledContexts ArgumentParserMethod => HandledContexts.None;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;

        public override string Summary => "Lists information about the current server";

        public ServerinfoCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }


        protected override Task ExecuteGuild(IGuildCommandContext context, object argObj)
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
            foreach (SocketGuildUser member in guild.Users)
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
}