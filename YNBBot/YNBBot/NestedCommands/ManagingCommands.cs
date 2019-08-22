using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.NestedCommands
{
    class SetUserNicknameCommand : Command
    {
        public const string SUMMARY = "Updates a users nickname";
        public const string REMARKS = default;
        public const string LINK = default;
        public static readonly Argument[] ARGS = new Argument[] {
            new Argument("User", "The user you want to assing a new nickname to"),
            new Argument("Nickname", "The nickname you want to assign. If it contains whitespace characters, encase in quotes!", multiple:true)
        };
        public static readonly Precondition[] PRECONDITIONS = new Precondition[] { AccessLevelAuthPrecondition.ADMIN };

        public SetUserNicknameCommand(string identifier) : base(identifier, OverriddenMethod.GuildSynchronous, OverriddenMethod.GuildAsync, false, ARGS, PRECONDITIONS, SUMMARY, REMARKS, LINK)
        {
        }

        SocketGuildUser TargetUser;
        string NewNickname;

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (!ArgumentParsing.TryParseGuildUser(context, context.Args.First, out TargetUser))
            {
                return new ArgumentParseResult(ARGS[0], $"Could not parse {context.Args.First} to a discord guild user!");
            }

            context.Args.Index++;

            NewNickname = ArgumentParsing.ParseMultiBlockArgument(context.Args);

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
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
    //        if (ArgumentParsing.TryParseGuildUser(context, context.Args.First, out Ban))
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
            if (!Enum.TryParse(context.Args.First, true, out Filter))
            {
                return new ArgumentParseResult(ARGS[0], $"Could not select a filter mode from `{context.Args.First}`!");
            }

            if (Filter == SelectFilter.User)
            {
                context.Args.Index++;
                if (!ArgumentParsing.TryParseGuildUser(context, context.Args.First, out User))
                {
                    return new ArgumentParseResult(ARGS[1], $"Could not select a user from `{context.Args.First}`!");
                }
            }

            context.Args.Index++;

            if (!Enum.TryParse(context.Args.First, true, out End))
            {
                return new ArgumentParseResult(ARGS[2], $"Could not select an end mode from `{context.Args.First}`!");
            }

            context.Args.Index++;

            if (context.Args.Count == 0)
            {
                return new ArgumentParseResult(ARGS[3], $"You need to supply an argument for the endmode!");
            }

            switch (End)
            {
                case EndMode.Count:
                    if (uint.TryParse(context.Args.First, out RemoveCount))
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
                        return new ArgumentParseResult(ARGS[3], $"Could not parse `{context.Args.First}` to a valid number!");
                    }
                    break;
                case EndMode.Time:
                    if (!DateTimeOffset.TryParseExact(context.Args.First, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DeleteAfter))
                    {
                        return new ArgumentParseResult(ARGS[3], $"Could not parse `{context.Args.First}` to a valid date and time. Required Format: `hh:mm` UTC!");
                    }
                    InitalCount = 1000;
                    break;
                case EndMode.TimeRelative:
                    if (Macros.TryParseHumanTimeString(context.Args.First, out TimeSpan maxAge))
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
                        return new ArgumentParseResult(ARGS[3], $"Could not parse `{context.Args.First}` to a valid timespan. Use Format `##h` or `##m`!");
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
}
