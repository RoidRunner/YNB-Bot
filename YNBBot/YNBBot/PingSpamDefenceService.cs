using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YNBBot.Interactive;

namespace YNBBot
{
    static class PingSpamDefenceService
    {
        /// <summary>
        /// Amount of effective mentions a user has to reach to trigger a warning
        /// </summary>
        public const int EM_WARNING_LIMIT = 20;
        /// <summary>
        /// Amount of effective mentions a user has to reach to get muted
        /// </summary>
        public const int EM_MUTE_LIMIT = 25;

        /// <summary>
        /// Timespan the limits are active for, in milliseconds (30 minutes)
        /// </summary>
        public const int EM_TIMESPAN = 1800000;

        private static List<MentionEvent> MentionEvents = new List<MentionEvent>();

        public static readonly EmbedBuilder MuteEmbed;
        public static readonly EmbedBuilder WarningEmbed;

        static PingSpamDefenceService()
        {
            MuteEmbed = new EmbedBuilder()
            {
                Title = "Muted for spamming Mentions",
                Description = "You have breached the limits in place to prevent raiding and excessive ping spamming. PM an admin if you believe this is in error!",
                Color = Var.ERRORCOLOR
            };
            WarningEmbed = new EmbedBuilder()
            {
                Title = "Warning for spamming Mentions",
                Description = "You have breached the warning limit in place to prevent excessive ping spamming. Avoid mentions in your next messages to avoid getting muted!",
                Color = Var.ERRORCOLOR
            };
        }

        /// <summary>
        /// Handles a message sent, and checks it for mentions
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task HandleMessage(SocketMessage message)
        {
            SocketUserMessage userMessage = message as SocketUserMessage;
            if (userMessage != null)
            {
                SocketGuildUser user = userMessage.Author as SocketGuildUser;
                if (user != null)
                {
                    SocketGuild guild = user.Guild;
                    AccessLevel userAccessLevel = Var.client.GetAccessLevel(user.Id);

                    int effectiveMentionCount = message.MentionedUsers.Count;
                    foreach (SocketRole role in message.MentionedRoles)
                    {
                        int memberCount = 0;
                        foreach (var member in role.Members)
                        {
                            memberCount++;
                        }
                        effectiveMentionCount += memberCount;
                    }

                    if (MessageMentionesEveryoneOrHere(message.Content))
                    {
                        effectiveMentionCount += guild.MemberCount;
                    }

                    if (effectiveMentionCount > 0 && userAccessLevel < AccessLevel.Admin && user.Id != Var.client.CurrentUser.Id)
                    {
                        await HandleMention(user, effectiveMentionCount);
                    }
                }
            }
        }

        /// <summary>
        /// Checks a string for mentions of @everyone and @here outside of codeblocks
        /// </summary>
        /// <param name="str">string to check</param>
        /// <returns>true, if it would create a notification</returns>
        public static bool MessageMentionesEveryoneOrHere(string str)
        {
            bool inlineCodeBlock = false;
            bool multiCodeBlock = false;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '`')
                {
                    bool multiMarker = false;
                    if (i + 2 < str.Length)
                    {
                        if (str[i+1] == '`' && str[i+2] == '`')
                        {
                            i += 2;
                            multiMarker = true;
                        }
                    }

                    if (multiMarker && !inlineCodeBlock)
                    {
                        multiCodeBlock = !multiCodeBlock;
                    }
                    if (!multiMarker && !multiCodeBlock)
                    {
                        inlineCodeBlock = !inlineCodeBlock;
                    }
                }
                else if (str[i] == '@' && !multiCodeBlock && !inlineCodeBlock)
                {
                    if (i + 8 < str.Length)
                    {
                        if (str.Substring(i, 9) == "@everyone")
                        {
                            return true;
                        }
                    }
                    if (i + 4 < str.Length)
                    {
                        if (str.Substring(i, 5) == "@here")
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Handles a mention event
        /// </summary>
        /// <param name="user">The user who sent the mentioning message</param>
        /// <param name="messageEMs">Amount of EMs inside that message</param>
        private static async Task HandleMention(SocketGuildUser user, int messageEMs)
        {
            bool firstInfraction = true;
            int totalEMs = messageEMs;


            List<MentionEvent> removeEvents = new List<MentionEvent>();
            foreach (MentionEvent oldEvent in MentionEvents)
            {
                if (!oldEvent.IsValid)
                {
                    removeEvents.Add(oldEvent);
                    continue;
                }
                if (oldEvent.UserId == user.Id)
                {
                    firstInfraction = false;
                    totalEMs += oldEvent.EffectiveMentions;
                }
            }
            foreach (MentionEvent removeEvent in removeEvents)
            {
                MentionEvents.Remove(removeEvent);
            }

            MentionEvent mentionEvent = new MentionEvent(user.Id, messageEMs);
            MentionEvents.Add(mentionEvent);

            if (totalEMs >= EM_MUTE_LIMIT && !firstInfraction)
            {
                // Handle Mute
                if (Var.client.TryGetRole(SettingsModel.MuteRole, out SocketRole MuteRole))
                {
                    await user.AddRoleAsync(MuteRole);
                    foreach (SocketRole role in user.Roles)
                    {
                        if (!role.IsEveryone && role.Id != MuteRole.Id)
                        {
                            await user.RemoveRoleAsync(role);
                        }
                    }
                }
                IDMChannel dmChannel = await user.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync(embed: MuteEmbed.Build());
                await AdminTaskInteractiveMessage.CreateAdminTaskMessage($"Muted User {user} for exceeding the EM limits", $"User: {user.Mention}\nEffective Mentions: `{totalEMs}/{EM_MUTE_LIMIT}`");
            }
            else if (totalEMs >= EM_WARNING_LIMIT)
            {
                // Handle Warning
                IDMChannel dmChannel = await user.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync(embed: WarningEmbed.Build());
            }
        }

        private struct MentionEvent
        {
            public ulong UserId;
            public int EffectiveMentions;
            public long Timestamp;

            public MentionEvent(ulong userId, int mentions)
            {
                UserId = userId;
                EffectiveMentions = mentions;
                Timestamp = TimingThread.Millis;
            }

            public bool IsValid
            {
                get
                {
                    return TimingThread.Millis - Timestamp < EM_TIMESPAN;
                }
            }
        }
    }
}
