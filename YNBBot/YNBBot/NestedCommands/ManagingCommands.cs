using BotCoreNET;
using BotCoreNET.CommandHandling;
using BotCoreNET.Helpers;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        // public static readonly Precondition[] PRECONDITIONS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public SetUserNicknameCommand(string identifier, CommandCollection collection) : base()
        {
            Register(identifier, collection);
        }

        SocketGuildUser TargetUser;
        string NewNickname;


        protected override Task<ArgumentParseResult> ParseArgumentsGuildAsync(IGuildCommandContext context)
        {
            if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out TargetUser))
            {
                return Task.FromResult(new ArgumentParseResult(Arguments[0], $"Could not parse {context.Arguments.First} to a discord guild user!"));
            }

            context.Arguments.Index++;

            NewNickname = context.Arguments.First;

            return Task.FromResult(ArgumentParseResult.SuccessfullParse);
        }

        protected override async Task ExecuteGuild(IGuildCommandContext context)
        {
            if (NewNickname == "reset")
            {
                NewNickname = null;
            }
            await TargetUser.ModifyAsync(GuildUserProperties => { GuildUserProperties.Nickname = NewNickname; });
            await context.Channel.SendEmbedAsync($"Successfully renamed {TargetUser.Mention}!");
        }
    }

    //class BanCommand : Command
    //{
    //    public BanCommand(string identifier)
    //         : base(identifier, AccessLevel.Admin, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync)
    //    {
    //        InitializeHelp("", new Argument[] { new Argument() });
    //    }

    //    SocketGuildUser Ban;
    //    protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
    //    {
    //        if (ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out Ban))
    //        {
    //            return ArgumentParseResult.SuccessfullParse;
    //        }
    //        else return new ArgumentParseResult("Failure");
    //    }

    //    protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
    //    {
    //        await context.Guild.AddBanAsync(Ban);
    //    }
    //}

    internal class PurgeMessagesCommand : Command
    {
        public const string SUMMARY = "Removes multiple messages following given criteria";
        public const string REMARKS = "The purge command has a maximum reach of 1000 messages into the past! (Use it multiple times if necessary)";
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("Filter", $"The filter to select messages to purge. Options:\n\n`{SelectFilter.All}` - No Filter\n`{SelectFilter.User}` - Messages by one user\n{SelectFilter.Text} - Remove only Text (ignores messages with attachments)\n`{SelectFilter.Mentions}` - Only Messages that contain mentions"),
            new Argument("FilterArgument", "Argument to control how the filter selects messages", optional:true),
            new Argument("EndMode", $"The mode to determine how many messages to purge. Options\n\n`{EndMode.Count}` - Specify the amount of messages you want deleted\n`{EndMode.Time}` - Only delete messages after the time in UTC `HH:MM`\n`{EndMode.TimeRelative}` - Only delete messages sent within `##h` hours or `##m` minutes (< 24h)"),
            new Argument("EndArgument", "Argument to control how the amount of messages to purge is determined")
        };
        public static readonly Precondition[] PRECONDITIONS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public PurgeMessagesCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, true, ARGS, PRECONDITIONS, SUMMARY, REMARKS, LINK)
        {
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
        SelectFilter Filter;
        EndMode End;
        SocketGuildUser User;

        int InitalCount;
        uint RemoveCount;
        DateTimeOffset DeleteAfter;

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (!Enum.TryParse(context.Arguments.First, true, out Filter))
            {
                return new ArgumentParseResult(ARGS[0], $"Could not select a filter mode from `{context.Arguments.First}`!");
            }

            if (Filter == SelectFilter.User)
            {
                context.Arguments.Index++;
                if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out User))
                {
                    return new ArgumentParseResult(ARGS[1], $"Could not select a user from `{context.Arguments.First}`!");
                }
            }

            context.Arguments.Index++;

            if (!Enum.TryParse(context.Arguments.First, true, out End))
            {
                return new ArgumentParseResult(ARGS[2], $"Could not select an end mode from `{context.Arguments.First}`!");
            }

            context.Arguments.Index++;

            if (context.Arguments.Count == 0)
            {
                return new ArgumentParseResult(ARGS[3], $"You need to supply an argument for the endmode!");
            }

            switch (End)
            {
                case EndMode.Count:
                    if (uint.TryParse(context.Arguments.First, out RemoveCount))
                    {
                        if (Filter == SelectFilter.All)
                        {
                            InitalCount = (int)RemoveCount;
                        }
                        else
                        {
                            InitalCount = 1000;
                        }
                    }
                    else
                    {
                        return new ArgumentParseResult(ARGS[3], $"Could not parse `{context.Arguments.First}` to a valid number!");
                    }
                    break;
                case EndMode.Time:
                    if (!DateTimeOffset.TryParseExact(context.Arguments.First, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DeleteAfter))
                    {
                        return new ArgumentParseResult(ARGS[3], $"Could not parse `{context.Arguments.First}` to a valid date and time. Required Format: `hh:mm` UTC!");
                    }
                    InitalCount = 1000;
                    break;
                case EndMode.TimeRelative:
                    if (Macros.TryParseHumanTimeString(context.Arguments.First, out TimeSpan maxAge))
                    {
                        if (maxAge.TotalHours > MAXHOURS)
                        {
                            return new ArgumentParseResult(ARGS[3], "Can not purge messages older than 24 hours!");
                        }
                        else
                        {
                            DeleteAfter = DateTimeOffset.UtcNow - maxAge;
                        }
                    }
                    else
                    {
                        return new ArgumentParseResult(ARGS[3], $"Could not parse `{context.Arguments.First}` to a valid timespan. Use Format `##h` or `##m`!");
                    }
                    InitalCount = 1000;
                    break;
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            int removals = 0;
            IEnumerable<IMessage> messages = await context.Channel.GetMessagesAsync(InitalCount, CacheMode.AllowDownload).FlattenAsync();
            foreach (IMessage message in messages)
            {
                if (IsMessageYoungEnough(message) && MessageMatchesSelectFilter(message))
                {
                    RemoveCount--;
                    removals++;
                    await context.Channel.DeleteMessageAsync(message);
                }
            }
            await context.Channel.SendEmbedAsync($"Purged Messages: {removals}");
        }

        bool IsMessageYoungEnough(IMessage message)
        {
            if (End == EndMode.Time || End == EndMode.TimeRelative)
            {
                return message.CreatedAt > DeleteAfter;
            }
            else
            {
                return RemoveCount > 0;
            }
        }

        bool MessageMatchesSelectFilter(IMessage message)
        {
            switch (Filter)
            {
                case SelectFilter.User:
                    return message.Author.Id == User?.Id;
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
        public const string SUMMARY = "Retrieves known modlogs for a given user!";
        public const string REMARKS = default;
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("User", "The user you to retrieve modlogs for"),
        };
        public static readonly Precondition[] PRECONDITIONS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public GetModLogsCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, true, ARGS, PRECONDITIONS, SUMMARY, REMARKS, LINK) { }

        private SocketGuildUser TargetUser = null;
        private ulong UserId = 0;

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out TargetUser))
            {
                if (!ulong.TryParse(context.Arguments.First, out UserId))
                {
                    TargetUser = null;
                    UserId = 0;
                    return new ArgumentParseResult(ARGS[0]);
                }
            }
            else
            {
                UserId = TargetUser.Id;
            }

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            GuildModerationLog guildModerationLog = GuildModerationLog.GetOrCreateGuildModerationLog(context.Guild.Id);
            UserModerationLog userModerationLog = guildModerationLog.GetOrCreateUserModerationLog(UserId);

            EmbedBuilder embed;
            if (TargetUser == null)
            {
                embed = new EmbedBuilder()
                {
                    Color = BotCore.EmbedColor,
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = UserId.ToString()
                    },
                };
            }
            else
            {
                embed = new EmbedBuilder()
                {
                    Color = BotCore.EmbedColor,
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = TargetUser.ToString(),
                        IconUrl = TargetUser.GetDefaultAvatarUrl(),
                        Url = TargetUser.GetDefaultAvatarUrl()
                    },
                };
            }

            if (TargetUser != null)
            {
                embed.AddField("User Information", $"Mention: {TargetUser.Mention}, ID: `{UserId}`");
            }

            string userStatus;
            if (userModerationLog.IsBanned)
            {
                if (userModerationLog.BannedUntil == DateTimeOffset.MaxValue)
                {
                    userStatus = "**Banned** permanently";
                }
                else
                {
                    userStatus = $"**Banned** until `{userModerationLog.BannedUntil.Value.ToString("u")} - ({userModerationLog.BanTimeRemaining.ToHumanTimeString()} remaining)`";
                }
            }
            else
            {
                if (userModerationLog.IsMuted)
                {
                    if (userModerationLog.MutedUntil == DateTimeOffset.MaxValue)
                    {
                        userStatus = "**Muted** until further notice";
                    }
                    else
                    {
                        userStatus = $"**Muted** until `{userModerationLog.MutedUntil.Value.ToString("u")} - ({userModerationLog.MuteTimeRemaining.ToHumanTimeString()} remaining)`";
                    }
                }
                else
                {
                    userStatus = $"No Infraction";
                }
            }
            embed.AddField("Status", userStatus);

            if (userModerationLog.ModerationEntries.Count == 0)
            {
                embed.AddField("Moderation Entries - 0", "No Moderation Entries present!");
            }
            else
            {
                embed.AddField($"Moderation Entries - {userModerationLog.ModerationEntries.Count}", string.Join('\n', userModerationLog.ModerationEntries));
            }

            await context.Channel.SendEmbedAsync(embed);
        }
    }

    internal class AddModLogNoteCommand : Command
    {
        public const string SUMMARY = "Adds a note to a users mod log";
        public const string REMARKS = default;
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("User", "The user you want to attach the note to"),
            new Argument("Note", "The note", multiple:true)
        };
        public static readonly Precondition[] PRECONDITIONS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public AddModLogNoteCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, true, ARGS, PRECONDITIONS, SUMMARY, REMARKS, LINK) { }

        private SocketGuildUser TargetUser = null;
        private ulong UserId = 0;
        private string Note = null;

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out TargetUser))
            {
                if (!ulong.TryParse(context.Arguments.First, out UserId))
                {
                    TargetUser = null;
                    UserId = 0;
                    return new ArgumentParseResult(ARGS[0]);
                }
            }
            else
            {
                UserId = TargetUser.Id;
            }

            Note = context.ContentSansIdentifier.Substring(Identifier.Length + context.Arguments.First.Length + 2);

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            GuildModerationLog guildModerationLog = GuildModerationLog.GetOrCreateGuildModerationLog(context.Guild.Id);
            UserModerationLog userModerationLog = guildModerationLog.GetOrCreateUserModerationLog(UserId);
            UserModerationEntry moderationEntry = new UserModerationEntry(context.Guild.Id, ModerationType.Note, null, context.GuildUser, Note);
            await userModerationLog.AddModerationEntry(moderationEntry);
            await context.Channel.SendEmbedAsync($"Added Note `{Note}` to modlogs for {(TargetUser == null ? Markdown.InlineCodeBlock(UserId.ToString()) : TargetUser.Mention)}");
        }
    }

    internal class KickUserCommand : Command
    {
        public const string SUMMARY = "Kicks a user from the server";
        public const string REMARKS = default;
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("User", "The user you want to kick"),
            new Argument("Reason", "Provide reason for your kick here!", multiple:true)
        };
        public static readonly Precondition[] PRECONDITIONS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public KickUserCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, true, ARGS, PRECONDITIONS, SUMMARY, REMARKS, LINK) { }

        private SocketGuildUser ToBeKicked;
        private string Reason;

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out ToBeKicked, allowSelf: false, allowName: false))
            {
                return new ArgumentParseResult(ARGS[0]);
            }

            Reason = context.ContentSansIdentifier.Substring(Identifier.Length + context.Arguments.First.Length + 2);

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            try
            {
                await ToBeKicked.KickAsync(Reason);
                GuildModerationLog guildModerationLog = GuildModerationLog.GetOrCreateGuildModerationLog(context.Guild.Id);
                UserModerationLog userModerationLog = guildModerationLog.GetOrCreateUserModerationLog(ToBeKicked.Id);
                UserModerationEntry moderationEntry = new UserModerationEntry(context.Guild.Id, ModerationType.Kicked, null, context.GuildUser, Reason);
                await userModerationLog.AddModerationEntry(moderationEntry);
                await context.Channel.SendEmbedAsync($"Kicked {ToBeKicked} for reason: `{Reason}`");

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
        //public static readonly Precondition[] PRECONDITIONS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

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

            argOut.Warning = context.RemoveArgumentsFront(1);

            return Task.FromResult(new ArgumentParseResult(argOut));
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            IDMChannel dmchannel = await TargetUser.GetOrCreateDMChannelAsync();
            await dmchannel.SendMessageAsync($"You have been warned on {context.Guild} for `{Warning}`!");
            GuildModerationLog guildModerationLog = GuildModerationLog.GetOrCreateGuildModerationLog(context.Guild.Id);
            UserModerationLog userModerationLog = guildModerationLog.GetOrCreateUserModerationLog(TargetUser.Id);
            UserModerationEntry moderationEntry = new UserModerationEntry(context.Guild.Id, ModerationType.Warning, null, context.GuildUser, Warning);
            await userModerationLog.AddModerationEntry(moderationEntry);
            await context.Channel.SendEmbedAsync($"Warned {TargetUser.Mention} with `{Warning}`");
        }
    }

    class BanUserCommand : Command
    {
        public const string SUMMARY = "Warns a user";
        public const string REMARKS = default;
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("User", "The user you want to ban"),
            new Argument("Duration", "How long you want the user banned for. Format: \n`perma` = Permaban\n`##D` = Tempban, Format for D: `m` = Minutes, `h` = Hours, `d` = Days, `M` = Months, `y` = Years"),
            new Argument("Reason", "Provide reason for your ban here!", multiple:true)
        };
        public static readonly Precondition[] PRECONDITIONS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public BanUserCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, true, ARGS, PRECONDITIONS, SUMMARY, REMARKS, LINK) { }

        private SocketGuildUser TargetUser = null;
        private DateTimeOffset BanEnds;
        private TimeSpan Duration;
        private string Reason = null;

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (!ArgumentParsing.TryParseGuildUser(context, context.Arguments.First, out TargetUser))
            {
                return new ArgumentParseResult(ARGS[0]);
            }

            context.Arguments.Index++;

            if (context.Arguments.First.ToLower() == "perma")
            {
                BanEnds = DateTimeOffset.MaxValue;
            }
            else
            {
                if (context.Arguments.First.Length == 1)
                {
                    return new ArgumentParseResult(ARGS[1]);
                }
                char format = context.Arguments.First[context.Arguments.First.Length - 1];
                string amount_str = context.Arguments.First.Substring(0, context.Arguments.First.Length - 1);
                if (double.TryParse(amount_str, out double amount))
                {
                    switch (format)
                    {
                        case 'm':
                            Duration = TimeSpan.FromMinutes(amount);
                            break;
                        case 'h':
                            Duration = TimeSpan.FromHours(amount);
                            break;
                        case 'd':
                            Duration = TimeSpan.FromDays(amount);
                            break;
                        case 'M':
                            Duration = TimeSpan.FromDays(amount * 30.4375);
                            break;
                        case 'y':
                            Duration = TimeSpan.FromDays(amount * 365.25);
                            break;
                        default:
                            return new ArgumentParseResult(ARGS[1]);
                    }
                    BanEnds = DateTimeOffset.UtcNow + Duration;
                }
                else
                {
                    return new ArgumentParseResult(ARGS[1]);
                }
            }

            Reason = context.ContentSansIdentifier.Substring(Identifier.Length + context.Arguments.First.Length + context.Arguments[-1].Length + 3);

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            IDMChannel dmchannel = await TargetUser.GetOrCreateDMChannelAsync();
            await dmchannel.SendMessageAsync($"You have been banned on {context.Guild} for `{Reason}`! {(BanEnds == DateTimeOffset.MaxValue ? "This is permanent." : $"You will be automatically unbanned in {Duration.ToHumanTimeString()} ({BanEnds.ToString("u")})")}");
            await TargetUser.BanAsync(reason: Reason);
            GuildModerationLog guildModerationLog = GuildModerationLog.GetOrCreateGuildModerationLog(context.Guild.Id);
            UserModerationLog userModerationLog = guildModerationLog.GetOrCreateUserModerationLog(TargetUser.Id);
            UserModerationEntry moderationEntry = new UserModerationEntry(context.Guild.Id, ModerationType.Banned, null, context.GuildUser, Reason, $"Duration: `{(BanEnds == DateTimeOffset.MaxValue ? "perma" : Duration.ToHumanTimeString())}`");
            await userModerationLog.AddBan(moderationEntry, BanEnds);
            await context.Channel.SendEmbedAsync($"Banned {TargetUser.Mention} for `{Reason}`");
        }
    }

    class UnBanUserCommand : Command
    {
        public const string SUMMARY = "Warns a user";
        public const string REMARKS = default;
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("User", "The id of the user you want to unban"),
            new Argument("Reason", "Why the ban was revoked", multiple:true)
        };
        public static readonly Precondition[] PRECONDITIONS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public UnBanUserCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, true, ARGS, PRECONDITIONS, SUMMARY, REMARKS, LINK) { }


    }
}
