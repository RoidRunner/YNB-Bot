using BotCoreNET;
using BotCoreNET.CommandHandling;
using BotCoreNET.Helpers;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YNBBot.Moderation;

namespace YNBBot.NestedCommands
{
    class SetUserNicknameCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;
        public override string Summary => "Updates a users nickname";
        public override Argument[] Arguments => new Argument[] {
            new Argument("User", "The user you want to assing a new nickname to"),
            new Argument("Nickname", "The nickname you want to assign. If it contains whitespace characters, encase in quotes!", multiple:true)
        };

        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(BotCore.ADMINROLE_BOTVARID) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;

        public SetUserNicknameCommand(string identifier, CommandCollection collection = null) : base()
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public SocketGuildUser TargetUser;
            public string NewNickname;
        }


        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer argOut = new ArgumentContainer();
            if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out argOut.TargetUser))
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[0], $"Could not parse {context.Arguments.First} to a discord guild user!"));
            }

            context.Arguments.Index++;

            argOut.NewNickname = context.Arguments.First;

            return Task.FromResult(new ArgumentParseResult(argOut));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;
            if (args.NewNickname == "reset")
            {
                args.NewNickname = null;
            }
            await args.TargetUser.ModifyAsync(GuildUserProperties => { GuildUserProperties.Nickname = args.NewNickname; });
            await context.Channel.SendEmbedAsync($"Successfully renamed {args.TargetUser.Mention}!");
        }
    }

    internal class PurgeMessagesCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;

        public override string Summary => "Removes multiple messages following given criteria";
        public override string Remarks => "The purge command has a maximum reach of 1000 messages into the past! (Use it multiple times if necessary)";
        public override Argument[] Arguments => new Argument[] {
            new Argument("Filter", $"The filter to select messages to purge. Options:\n\n`{SelectFilter.All}` - No Filter\n`{SelectFilter.User}` - Messages by one user\n{SelectFilter.Text} - Remove only Text (ignores messages with attachments)\n`{SelectFilter.Mentions}` - Only Messages that contain mentions"),
            new Argument("FilterArgument", "Argument to control how the filter selects messages", optional:true),
            new Argument("EndMode", $"The mode to determine how many messages to purge. Options\n\n`{EndMode.Count}` - Specify the amount of messages you want deleted\n`{EndMode.Time}` - Only delete messages after the time in UTC `HH:MM`\n`{EndMode.TimeRelative}` - Only delete messages sent within `##h` hours or `##m` minutes (< 24h)"),
            new Argument("EndArgument", "Argument to control how the amount of messages to purge is determined")
        };

        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(BotCore.ADMINROLE_BOTVARID) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;

        public override bool RunInAsyncMode => true;

        public PurgeMessagesCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        enum SelectFilter
        {
            User,
            All,
            Text,
            Mentions
        }

        enum EndMode
        {
            Count,
            Time,
            TimeRelative
        }

        private const int MAXHOURS = 24;

        private class ArgumentContainer
        {
            public SelectFilter Filter;
            public EndMode End;
            public SocketGuildUser User;

            public int InitalCount;
            public uint RemoveCount;
            public DateTimeOffset DeleteAfter;
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer argOut = new ArgumentContainer();

            if (!Enum.TryParse(context.Arguments.First, true, out argOut.Filter))
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[0], $"Could not select a filter mode from `{context.Arguments.First}`!"));
            }

            if (argOut.Filter == SelectFilter.User)
            {
                context.Arguments.Index++;
                if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out argOut.User))
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[1], $"Could not select a user from `{context.Arguments.First}`!"));
                }
            }

            context.Arguments.Index++;

            if (!Enum.TryParse(context.Arguments.First, true, out argOut.End))
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[2], $"Could not select an end mode from `{context.Arguments.First}`!"));
            }

            context.Arguments.Index++;

            if (context.Arguments.Count == 0)
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[3], $"You need to supply an argument for the endmode!"));
            }

            switch (argOut.End)
            {
                case EndMode.Count:
                    if (uint.TryParse(context.Arguments.First, out argOut.RemoveCount))
                    {
                        if (argOut.Filter == SelectFilter.All)
                        {
                            argOut.InitalCount = (int)argOut.RemoveCount;
                        }
                        else
                        {
                            argOut.InitalCount = 1000;
                        }
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(Arguments[3], $"Could not parse `{context.Arguments.First}` to a valid number!"));
                    }
                    break;
                case EndMode.Time:
                    if (!DateTimeOffset.TryParseExact(context.Arguments.First, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out argOut.DeleteAfter))
                    {
                        return Task.FromResult(new ArgumentParseResult(Arguments[3], $"Could not parse `{context.Arguments.First}` to a valid date and time. Required Format: `hh:mm` UTC!"));
                    }
                    argOut.InitalCount = 1000;
                    break;
                case EndMode.TimeRelative:
                    if (Macros.TryParseHumanTimeString(context.Arguments.First, out TimeSpan maxAge))
                    {
                        if (maxAge.TotalHours > MAXHOURS)
                        {
                            return Task.FromResult(new ArgumentParseResult(Arguments[3], "Can not purge messages older than 24 hours!"));
                        }
                        else
                        {
                            argOut.DeleteAfter = DateTimeOffset.UtcNow - maxAge;
                        }
                    }
                    else
                    {
                        return Task.FromResult(new ArgumentParseResult(Arguments[3], $"Could not parse `{context.Arguments.First}` to a valid timespan. Use Format `##h` or `##m`!"));
                    }
                    argOut.InitalCount = 1000;
                    break;
            }

            return Task.FromResult(new ArgumentParseResult(argOut));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            int removals = 0;
            IEnumerable<IMessage> messages = await context.Channel.GetMessagesAsync(args.InitalCount, CacheMode.AllowDownload).FlattenAsync();
            foreach (IMessage message in messages)
            {
                if (IsMessageYoungEnough(message, args) && MessageMatchesSelectFilter(message, args))
                {
                    args.RemoveCount--;
                    removals++;
                    await context.Channel.DeleteMessageAsync(message);
                }
            }
            await context.Channel.SendEmbedAsync($"Purged Messages: {removals}");
        }

        bool IsMessageYoungEnough(IMessage message, ArgumentContainer args)
        {
            if (args.End == EndMode.Time || args.End == EndMode.TimeRelative)
            {
                return message.CreatedAt > args.DeleteAfter;
            }
            else
            {
                return args.RemoveCount > 0;
            }
        }

        bool MessageMatchesSelectFilter(IMessage message, ArgumentContainer args)
        {
            switch (args.Filter)
            {
                case SelectFilter.User:
                    return message.Author.Id == args.User?.Id;
                case SelectFilter.All:
                    return true;
                case SelectFilter.Text:
                    return message.Attachments.Count == 0;
                case SelectFilter.Mentions:
                    return message.MentionedRoleIds.Count > 0 || message.MentionedUserIds.Count > 0 || PingSpamDefenceService.MessageMentionesEveryoneOrHere(message.Content);
            }
            return false;
        }
    }

    internal class GetModLogsCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;

        public override string Summary => "Retrieves known modlogs for a given user!";
        public override Argument[] Arguments => new Argument[] {
            new Argument("User", "The user you to retrieve modlogs for"),
        };

        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(BotCore.ADMINROLE_BOTVARID) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;

        public GetModLogsCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public SocketGuildUser TargetUser = null;
            public ulong UserId = 0;
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer argOut = new ArgumentContainer();
            if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out argOut.TargetUser))
            {
                if (!ulong.TryParse(context.Arguments.First, out argOut.UserId))
                {
                    argOut.TargetUser = null;
                    argOut.UserId = 0;
                    return Task.FromResult(new ArgumentParseResult(Arguments[0]));
                }
            }
            else
            {
                argOut.UserId = argOut.TargetUser.Id;
            }

            return Task.FromResult(new ArgumentParseResult(argOut));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            GuildModerationLog guildModerationLog = GuildModerationLog.GetOrCreateGuildModerationLog(context.Guild.Id);
            UserModerationLog userModerationLog = guildModerationLog.GetOrCreateUserModerationLog(args.UserId);

            AutoExpandingMessage embed;
            if (args.TargetUser == null)
            {
                embed = new AutoExpandingMessage()
                {
                    Color = BotCore.EmbedColor,
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = args.UserId.ToString()
                    },
                };
            }
            else
            {
                embed = new AutoExpandingMessage()
                {
                    Color = BotCore.EmbedColor,
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = args.TargetUser.ToString(),
                        IconUrl = args.TargetUser.GetDefaultAvatarUrl(),
                        Url = args.TargetUser.GetDefaultAvatarUrl()
                    },
                };
            }

            string description = string.Empty;
            
            if (args.TargetUser != null)
            {
                description += $"**User Information:** Mention: {args.TargetUser.Mention}, ID: `{args.UserId}`\n";
            }
            else
            {
                description += $"**User Information:** ID: `{args.UserId}`\n";
            }

            string userStatus;
            if (userModerationLog.IsBanned)
            {
                if (userModerationLog.BannedUntil == DateTimeOffset.MaxValue)
                {
                    userStatus = "**Banned** permanently\n";
                }
                else
                {
                    userStatus = $"**Banned** until `{userModerationLog.BannedUntil.Value.ToString("u")} - ({userModerationLog.BanTimeRemaining.ToHumanTimeString()} remaining)`\n";
                }
            }
            else
            {
                if (userModerationLog.IsMuted)
                {
                    if (userModerationLog.MutedUntil == DateTimeOffset.MaxValue)
                    {
                        userStatus = "**Muted** until further notice\n";
                    }
                    else
                    {
                        userStatus = $"**Muted** until `{userModerationLog.MutedUntil.Value.ToString("u")} - ({userModerationLog.MuteTimeRemaining.ToHumanTimeString()} remaining)`\n";
                    }
                }
                else
                {
                    userStatus = $"No Infraction\n";
                }
            }
            description += $"**Status:** {userStatus}\n\n**Moderation Entries**: {userModerationLog.ModerationEntries.Count}";

            embed.Description = description;

            foreach (UserModerationEntry entry in userModerationLog.ModerationEntries) 
            {
                embed.AddLine(entry);
            }

            await embed.Send(context.Channel);
        }
    }

    internal class AddModLogNoteCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;

        public override string Summary => "Adds a note to a users mod log";
        public const string REMARKS = default;
        public const string LINK = default;
        public override Argument[] Arguments => new Argument[] {
            new Argument("User", "The user you want to attach the note to"),
            new Argument("Note", "The note", multiple:true)
        };

        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(BotCore.ADMINROLE_BOTVARID) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;

        public AddModLogNoteCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public SocketGuildUser TargetUser = null;
            public ulong UserId = 0;
            public string Note = null;
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer argOut = new ArgumentContainer();

            if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out argOut.TargetUser))
            {
                if (!ulong.TryParse(context.Arguments.First, out argOut.UserId))
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[0]));
                }
            }
            else
            {
                argOut.UserId = argOut.TargetUser.Id;
            }

            context.Arguments.Index++;

            argOut.Note = context.Arguments.First;

            return Task.FromResult(new ArgumentParseResult(argOut));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            GuildModerationLog guildModerationLog = GuildModerationLog.GetOrCreateGuildModerationLog(context.Guild.Id);
            UserModerationLog userModerationLog = guildModerationLog.GetOrCreateUserModerationLog(args.UserId);
            UserModerationEntry moderationEntry = new UserModerationEntry(context.Guild.Id, ModerationType.Note, null, context.GuildUser, args.Note);
            await userModerationLog.AddModerationEntry(moderationEntry);
            await context.Channel.SendEmbedAsync($"Added Note `{args.Note}` to modlogs for {(args.TargetUser == null ? Markdown.InlineCodeBlock(args.UserId.ToString()) : args.TargetUser.Mention)}");
        }
    }

    internal class KickUserCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;

        public override string Summary => "Kicks a user from the server";
        public override Argument[] Arguments => new Argument[] {
            new Argument("User", "The user you want to kick"),
            new Argument("Reason", "Provide reason for your kick here!", multiple:true)
        };

        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(BotCore.ADMINROLE_BOTVARID) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;

        public KickUserCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public SocketGuildUser ToBeKicked;
            public string Reason;
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer argOut = new ArgumentContainer();

            if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out argOut.ToBeKicked, allowSelf: false, allowName: false))
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[0]));
            }

            context.Arguments.Index++;

            argOut.Reason = context.Arguments.First;

            return Task.FromResult(new ArgumentParseResult(argOut));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            try
            {
                await args.ToBeKicked.KickAsync(args.Reason);
                GuildModerationLog guildModerationLog = GuildModerationLog.GetOrCreateGuildModerationLog(context.Guild.Id);
                UserModerationLog userModerationLog = guildModerationLog.GetOrCreateUserModerationLog(args.ToBeKicked.Id);
                UserModerationEntry moderationEntry = new UserModerationEntry(context.Guild.Id, ModerationType.Kicked, null, context.GuildUser, args.Reason);
                await userModerationLog.AddModerationEntry(moderationEntry);
                await context.Channel.SendEmbedAsync($"Kicked {args.ToBeKicked} for reason: `{args.Reason}`");

            }
            catch (HttpException e)
            {
                await context.Channel.SendEmbedAsync($"Failed to kick due to \"{e.Reason}\"!", true);
            }
        }
    }

    class WarnUserCommand : Command
    {
        public override Argument[] Arguments => new Argument[] {
            new Argument("User", "The user you want to warn"),
            new Argument("Warning", "Provide reason for your warning here!", multiple:true)
        };

        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(BotCore.ADMINROLE_BOTVARID) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;

        public WarnUserCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public SocketGuildUser TargetUser = null;
            public string Warning = null;
        }

        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;

        public override string Summary => "Warns a user";

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer argOut = new ArgumentContainer();
            if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out argOut.TargetUser))
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[0]));
            }

            context.Arguments.Index++;

            argOut.Warning = context.Arguments.First;

            return Task.FromResult(new ArgumentParseResult(argOut));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;
            IDMChannel dmchannel = await args.TargetUser.GetOrCreateDMChannelAsync();
            await dmchannel.SendMessageAsync($"You have been warned on {context.Guild} for `{args.Warning}`!");
            GuildModerationLog guildModerationLog = GuildModerationLog.GetOrCreateGuildModerationLog(context.Guild.Id);
            UserModerationLog userModerationLog = guildModerationLog.GetOrCreateUserModerationLog(args.TargetUser.Id);
            UserModerationEntry moderationEntry = new UserModerationEntry(context.Guild.Id, ModerationType.Warning, null, context.GuildUser, args.Warning);
            await userModerationLog.AddModerationEntry(moderationEntry);
            await context.Channel.SendEmbedAsync($"Warned {args.TargetUser.Mention} with `{args.Warning}`");
        }
    }

    class BanUserCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;

        public override string Summary => "Bans a user";
        public override Argument[] Arguments => new Argument[] {
            new Argument("User", "The user you want to ban"),
            new Argument("Duration", "How long you want the user banned for. Format: \n`perma` = Permaban\n`##D` = Tempban, Format for D: `m` = Minutes, `h` = Hours, `d` = Days, `M` = Months, `y` = Years"),
            new Argument("Reason", "Provide reason for your ban here!", multiple:true)
        };

        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(BotCore.ADMINROLE_BOTVARID) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;

        public BanUserCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public SocketGuildUser TargetUser = null;
            public DateTimeOffset BanEnds;
            public TimeSpan Duration;
            public string Reason = null;
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer argOut = new ArgumentContainer();

            if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out argOut.TargetUser, allowName:false, allowSelf:false))
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[0]));
            }

            context.Arguments.Index++;

            if (context.Arguments.First.ToLower() == "perma")
            {
                argOut.BanEnds = DateTimeOffset.MaxValue;
            }
            else
            {
                if (context.Arguments.First.Length == 1)
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[1]));
                }
                char format = context.Arguments.First[context.Arguments.First.Length - 1];
                string amount_str = context.Arguments.First.Substring(0, context.Arguments.First.Length - 1);
                if (double.TryParse(amount_str, out double amount))
                {
                    switch (format)
                    {
                        case 'm':
                            argOut.Duration = TimeSpan.FromMinutes(amount);
                            break;
                        case 'h':
                            argOut.Duration = TimeSpan.FromHours(amount);
                            break;
                        case 'd':
                            argOut.Duration = TimeSpan.FromDays(amount);
                            break;
                        case 'M':
                            argOut.Duration = TimeSpan.FromDays(amount * 30.4375);
                            break;
                        case 'y':
                            argOut.Duration = TimeSpan.FromDays(amount * 365.25);
                            break;
                        default:
                            return Task.FromResult(new ArgumentParseResult(Arguments[1]));
                    }
                    argOut.BanEnds = DateTimeOffset.UtcNow + argOut.Duration;
                }
                else
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[1]));
                }
            }

            context.Arguments.Index++;

            argOut.Reason = context.Arguments.First;

            return Task.FromResult(new ArgumentParseResult(argOut));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object argObj)
        {
            ArgumentContainer args = argObj as ArgumentContainer;

            IDMChannel dmchannel = await args.TargetUser.GetOrCreateDMChannelAsync();
            await dmchannel.SendMessageAsync($"You have been banned on {context.Guild} for `{args.Reason}`! {(args.BanEnds == DateTimeOffset.MaxValue ? "This is permanent." : $"You will be automatically unbanned in {args.Duration.ToHumanTimeString()} ({args.BanEnds.ToString("u")})")}");
            await args.TargetUser.BanAsync(reason: args.Reason);
            GuildModerationLog guildModerationLog = GuildModerationLog.GetOrCreateGuildModerationLog(context.Guild.Id);
            UserModerationLog userModerationLog = guildModerationLog.GetOrCreateUserModerationLog(args.TargetUser.Id);
            UserModerationEntry moderationEntry = new UserModerationEntry(context.Guild.Id, ModerationType.Banned, null, context.GuildUser, args.Reason, $"Duration: `{(args.BanEnds == DateTimeOffset.MaxValue ? "perma" : args.Duration.ToHumanTimeString())}`");
            await userModerationLog.AddBan(moderationEntry, args.BanEnds);
            await context.Channel.SendEmbedAsync($"Banned {args.TargetUser.Mention} for `{args.Reason}`");
        }
    }

    class UnBanUserCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;

        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;

        public override string Summary => "Warns a user";
        public override Argument[] Arguments => ARGS;

        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("User", "The id of the user you want to unban"),
            new Argument("Reason", "Why the ban was revoked", multiple:true)
        };

        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(BotCore.ADMINROLE_BOTVARID) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;

        public UnBanUserCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public ulong UserId;
            public string Reason;
        }

        protected override async Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer args = new ArgumentContainer();
            if (!ulong.TryParse(context.Arguments.First, out args.UserId))
            {
                return new ArgumentParseResult(ARGS[1]);
            }

            var bans = await context.Guild.GetBansAsync();

            if (!bans.Any(ban => { return ban.User.Id == args.UserId; }))
            {
                return new ArgumentParseResult(ARGS[1], "This user is not banned!");
            }

            context.Arguments.Index++;

            args.Reason = context.Arguments.First;

            return new ArgumentParseResult(args);
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object parsedArgs)
        {
            ArgumentContainer args = parsedArgs as ArgumentContainer;
            await context.Guild.RemoveBanAsync(args.UserId);
            GuildModerationLog guildLog = GuildModerationLog.GetOrCreateGuildModerationLog(context.Guild.Id);
            UserModerationLog userLog = guildLog.GetOrCreateUserModerationLog(args.UserId);
            var entry = new UserModerationEntry(context.Guild.Id, ModerationType.UnBanned, null, context.GuildUser, args.Reason);
            await userLog.UnBan(context.Guild, entry);
            await context.Channel.SendEmbedAsync($"Unbanned `{args.UserId}`");
        }
    }

    class MuteUserCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;
        public override string Summary => "Mutes a user";

        public override Argument[] Arguments => ARGS;
        private static readonly Argument[] ARGS = new Argument[] 
        {
            new Argument("User", "The user you want banned"),
            new Argument("Timespan", "How long the user is meant to stay muted for"),
            new Argument("Reason", "Why the user in question is muted")
        };


        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(BotCore.ADMINROLE_BOTVARID) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;
        public override bool RunInAsyncMode => true;

        public MuteUserCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public SocketGuildUser TargetUser;
            public DateTimeOffset MuteUntil;
            public TimeSpan Duration;
            public string Reason;
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer args = new ArgumentContainer();

            if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out args.TargetUser, allowName:false, allowSelf: false))
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[0]));
            }

            context.Arguments.Index++;

            if (context.Arguments.First.ToLower() == "perma")
            {
                args.MuteUntil = DateTimeOffset.MaxValue;
            }
            else
            {
                if (context.Arguments.First.Length == 1)
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[1]));
                }
                char format = context.Arguments.First[context.Arguments.First.Length - 1];
                string amount_str = context.Arguments.First.Substring(0, context.Arguments.First.Length - 1);
                if (double.TryParse(amount_str, out double amount))
                {
                    switch (format)
                    {
                        case 'm':
                            args.Duration = TimeSpan.FromMinutes(amount);
                            break;
                        case 'h':
                            args.Duration = TimeSpan.FromHours(amount);
                            break;
                        case 'd':
                            args.Duration = TimeSpan.FromDays(amount);
                            break;
                        case 'M':
                            args.Duration = TimeSpan.FromDays(amount * 30.4375);
                            break;
                        case 'y':
                            args.Duration = TimeSpan.FromDays(amount * 365.25);
                            break;
                        default:
                            return Task.FromResult(new ArgumentParseResult(Arguments[1]));
                    }
                    args.MuteUntil = DateTimeOffset.UtcNow + args.Duration;
                }
                else
                {
                    return Task.FromResult(new ArgumentParseResult(Arguments[1]));
                }
            }

            context.Arguments.Index++;

            args.Reason = context.Arguments.First;

            return Task.FromResult(new ArgumentParseResult(args));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object parsedArgs)
        {
            ArgumentContainer args = parsedArgs as ArgumentContainer;

            // Generate UserModerationEntry

            UserModerationEntry entry;
            if (args.MuteUntil == DateTimeOffset.MaxValue)
            {
                entry = new UserModerationEntry(context.Guild.Id, ModerationType.Muted, DateTimeOffset.UtcNow, context.GuildUser, args.Reason, "Duration: perma");
            }
            else
            {
                entry = new UserModerationEntry(context.Guild.Id, ModerationType.Muted, DateTimeOffset.UtcNow, context.GuildUser, args.Reason, "Duration: " + args.Duration.ToHumanTimeString());
            }
            UserModerationLog userLog = GuildModerationLog.GetOrCreateUserModerationLog(context.Guild.Id, args.TargetUser.Id, out _);

            await userLog.AddMute(args.TargetUser, args.MuteUntil, entry);

            // Report success

            await context.Channel.SendEmbedAsync($"Muted {args.TargetUser.Mention} ({args.TargetUser.Id}) for `{args.Reason}`");
        }
    }

    class UnMuteUserCommand : Command
    {
        public override HandledContexts ArgumentParserMethod => HandledContexts.GuildOnly;
        public override HandledContexts ExecutionMethod => HandledContexts.GuildOnly;
        public override string Summary => "Unmutes a user";

        public override Argument[] Arguments => ARGS;
        private static readonly Argument[] ARGS = new Argument[]
        {
            new Argument("User", "The user you want to unmute"),
            new Argument("Reason", "Why the user in question is unmuted")
        };


        private static readonly Precondition[] PRECONDITIONS = new Precondition[] { new HasRolePrecondition(BotCore.ADMINROLE_BOTVARID) };
        public override Precondition[] ExecutePreconditions => PRECONDITIONS;
        public override Precondition[] ViewPreconditions => PRECONDITIONS;
        private static readonly Precondition[] PREC = new Precondition[] { new IsOwnerOrAdminPrecondition() };
        public override bool RunInAsyncMode => true;

        public UnMuteUserCommand(string identifier, CommandCollection collection = null)
        {
            Register(identifier, collection);
        }

        private class ArgumentContainer
        {
            public SocketGuildUser TargetUser;
            public string Reason;
        }

        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            ArgumentContainer args = new ArgumentContainer();

            if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out args.TargetUser, allowSelf:false, allowName: false))
            {
                return Task.FromResult(new ArgumentParseResult(ARGS[0]));
            }

            context.Arguments.Index++;

            args.Reason = context.Arguments.First;

            return Task.FromResult(new ArgumentParseResult(args));
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context, object parsedArgs)
        {
            ArgumentContainer args = parsedArgs as ArgumentContainer;
            UserModerationLog userLog = GuildModerationLog.GetOrCreateUserModerationLog(context.Guild.Id, args.TargetUser.Id, out _);
            try
            {
                UserModerationEntry entry = new UserModerationEntry(context.Guild.Id, ModerationType.UnMuted, null, context.GuildUser, args.Reason);
                await userLog.RemoveMute(args.TargetUser, entry);
            }
            catch (Exception e)
            {
                await context.Channel.SendEmbedAsync("Failed to manage roles: " + e.Message, true);
            }

            await context.Channel.SendEmbedAsync($"Unmuted {args.TargetUser.Mention} ({args.TargetUser.Id}) with reason `{args.Reason}`");
        }
    }
}
