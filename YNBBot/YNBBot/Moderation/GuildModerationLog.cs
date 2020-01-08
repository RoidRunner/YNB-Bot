using BotCoreNET;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YNBBot.Moderation
{
    class GuildModerationLog
    {
        #region static
        #region GuildModerationLogs

        private static Dictionary<ulong, GuildModerationLog> GuildLogs = new Dictionary<ulong, GuildModerationLog>();

        public static bool TryGetGuildModerationLog(ulong guildId, out GuildModerationLog guildModerationLog)
        {
            return GuildLogs.TryGetValue(guildId, out guildModerationLog);
        }

        public static GuildModerationLog GetOrCreateGuildModerationLog(ulong guildId)
        {
            if (GuildLogs.TryGetValue(guildId, out GuildModerationLog guildModerationLog))
            {
                return guildModerationLog;
            }
            else
            {
                guildModerationLog = new GuildModerationLog(guildId);
                GuildLogs.Add(guildId, guildModerationLog);
                return guildModerationLog;
            }
        }

        public static UserModerationLog GetOrCreateUserModerationLog(ulong guildId, ulong userId, out GuildModerationLog guildLog)
        {
            guildLog = GetOrCreateGuildModerationLog(guildId);
            return guildLog.GetOrCreateUserModerationLog(userId);
        }

        public static ChannelModerationLog GetOrCreateChannelModerationLog(ulong guildId, ulong channelId, out GuildModerationLog guildLog)
        {
            guildLog = GetOrCreateGuildModerationLog(guildId);
            return guildLog.GetOrCreateChannelModerationLog(channelId);
        }

        public static async Task LoadModerationLogs()
        {
            if (Directory.Exists(ResourcesModel.ModerationLogsPath))
            {
                foreach (var directory in Directory.EnumerateDirectories(ResourcesModel.ModerationLogsPath))
                {
                    string[] guildId_str = directory.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (ulong.TryParse(guildId_str[guildId_str.Length - 1], out ulong guildId))
                    {
                        await LoadGuildModerationLog(guildId);
                    }
                }
            }
        }

        private static async Task LoadGuildModerationLog(ulong guildId)
        {
            GuildModerationLog guildModerationLog = new GuildModerationLog(guildId);
            if (Directory.Exists(guildModerationLog.UserDirectory))
            {
                foreach (string filepath in Directory.EnumerateFiles(guildModerationLog.UserDirectory, "*.json"))
                {
                    LoadFileOperation load = await ResourcesModel.LoadToJSONObject(filepath);
                    if (load.Success)
                    {
                        UserModerationLog userModerationLog = new UserModerationLog(guildModerationLog);
                        if (userModerationLog.FromJSON(load.Result))
                        {
                            guildModerationLog.userLogs.Add(userModerationLog.UserId, userModerationLog);
                            if (userModerationLog.IsBanned)
                            {
                                if (userModerationLog.BannedUntil.Value < DateTimeOffset.MaxValue)
                                {
                                    AddTimeLimitedInfractionReference(userModerationLog);
                                }
                            }
                            else if (userModerationLog.IsMuted)
                            {
                                if (userModerationLog.MutedUntil.Value < DateTimeOffset.MaxValue)
                                {
                                    AddTimeLimitedInfractionReference(userModerationLog);
                                }
                            }
                        }
                    }
                }
            }
            if (Directory.Exists(guildModerationLog.ChannelDirectory))
            {
                foreach (string filepath in Directory.EnumerateFiles(guildModerationLog.ChannelDirectory, "*.json"))
                {
                    LoadFileOperation load = await ResourcesModel.LoadToJSONObject(filepath);
                    if (load.Success)
                    {
                        ChannelModerationLog channelModerationLog = new ChannelModerationLog(guildModerationLog);
                        if (channelModerationLog.FromJSON(load.Result))
                        {
                            guildModerationLog.channelLogs.Add(channelModerationLog.ChannelId, channelModerationLog);
                        }
                    }
                }
            }
            GuildLogs.Add(guildModerationLog.GuildId, guildModerationLog);
        }

        #endregion
        #region UpdateThread

        private static readonly Thread updateThread;
        private static readonly object addlistlock = new object();
        private static readonly object removelistlock = new object();
        private static readonly List<UserModerationLog> timeLimitedInfractions = new List<UserModerationLog>();
        private static readonly List<UserModerationLog> toBeAdded = new List<UserModerationLog>();
        private static readonly List<UserModerationLog> toBeRemoved = new List<UserModerationLog>();

        static GuildModerationLog()
        {
            updateThread = new Thread(new ThreadStart(CheckTimeLimitedInfractions));
            updateThread.Start();
        }

        internal static IReadOnlyList<UserModerationLog> TimeLimitedInfractionReferences => timeLimitedInfractions.AsReadOnly();

        public static void AddTimeLimitedInfractionReference(UserModerationLog usermodlog)
        {
            lock (addlistlock)
            {
                toBeAdded.Add(usermodlog);
            }
        }

        public static void RemoveTimeLimitedInfractionReference(UserModerationLog usermodlog)
        {
            lock (removelistlock)
            {
                toBeRemoved.Add(usermodlog);
            }
        }

        private static async void CheckTimeLimitedInfractions()
        {
            TimeSpan waitDelay = TimeSpan.FromMinutes(1);
            Thread.Sleep(waitDelay);
            while (true)
            {
                foreach (UserModerationLog userModlog in timeLimitedInfractions)
                {
                    try
                    {
                        if (userModlog.IsBanned)
                        {
                            if (userModlog.BannedUntil.Value < DateTimeOffset.UtcNow)
                            {
                                // Unban User
                                await UnbanUser(userModlog);
                                lock (removelistlock)
                                {
                                    toBeRemoved.Add(userModlog);
                                }
                            }
                        }
                        else if (userModlog.IsMuted)
                        {
                            if (userModlog.MutedUntil.Value < DateTimeOffset.UtcNow)
                            {
                                // Unmute User
                                await UnMuteUser(userModlog);
                                lock (removelistlock)
                                {
                                    toBeRemoved.Add(userModlog);
                                }
                            }
                        }
                        else
                        {
                            lock (removelistlock)
                            {
                                toBeRemoved.Add(userModlog);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        await GuildChannelHelper.SendExceptionNotification(e, string.Empty);
                    }
                }
                lock (removelistlock)
                {
                    timeLimitedInfractions.RemoveRange(toBeRemoved);
                    toBeRemoved.Clear();
                }
                lock (addlistlock)
                {
                    timeLimitedInfractions.AddRange(toBeAdded);
                    toBeAdded.Clear();
                }
                Thread.Sleep(waitDelay);
            }
        }

        private static async Task<string> UnbanUser(UserModerationLog userModLog)
        {
            GuildModerationLog guildLog = userModLog.Parent;
            SocketGuild guild = BotCore.Client.GetGuild(guildLog.GuildId);

            if (guild != null)
            {
                await userModLog.UnBan(guild);
                return null;
            }
            else
            {
                return $"Failed to unban `{userModLog.UserId}` - Guild `{userModLog.Parent.GuildId}` not found!";
            }
        }

        private static async Task<string> UnMuteUser(UserModerationLog userModLog)
        {
            GuildModerationLog guildLog = userModLog.Parent;
            SocketGuild guild = BotCore.Client.GetGuild(guildLog.GuildId);
            if (guild is null)
            {
                return $"Failed to unmute `{userModLog.UserId}` - Guild `{userModLog.Parent.GuildId}` not found!";
            }
            SocketGuildUser user = guild.GetUser(userModLog.UserId);
            if (user is null)
            {
                return $"Failed to unmute `{userModLog.UserId}` - User not found!";
            }

            await userModLog.RemoveMute(user);
            return null;
        }

        #endregion
        #endregion

        private Dictionary<ulong, UserModerationLog> userLogs = new Dictionary<ulong, UserModerationLog>();
        private Dictionary<ulong, ChannelModerationLog> channelLogs = new Dictionary<ulong, ChannelModerationLog>();

        public readonly ulong GuildId;
        public readonly string UserDirectory;
        public readonly string ChannelDirectory;

        public GuildModerationLog(ulong guildId)
        {
            GuildId = guildId;
            UserDirectory = $"{ResourcesModel.ModerationLogsPath}/{guildId}/Users";
            ChannelDirectory = $"{ResourcesModel.ModerationLogsPath}/{guildId}/Channels";
            AssurePath();
        }

        public void AssurePath()
        {
            if (!Directory.Exists(UserDirectory))
            {
                Directory.CreateDirectory(UserDirectory);
            }
        }

        public UserModerationLog GetOrCreateUserModerationLog(ulong userId)
        {
            if (!userLogs.TryGetValue(userId, out UserModerationLog usermoderationLog))
            {
                usermoderationLog = new UserModerationLog(this, userId);
                userLogs.Add(userId, usermoderationLog);
            }
            return usermoderationLog;
        }

        public ChannelModerationLog GetOrCreateChannelModerationLog(ulong channelId)
        {
            if (!channelLogs.TryGetValue(channelId, out ChannelModerationLog channelModerationLog))
            {
                channelModerationLog = new ChannelModerationLog(this, channelId);
                channelLogs.Add(channelId, channelModerationLog);
            }
            return channelModerationLog;
        }

        public delegate Task OnUserModerationLogModifiedDelegate(GuildModerationLog guildLog, UserModerationLog userLog, UserModerationEntry entry);

        public static event OnUserModerationLogModifiedDelegate OnUserModerationLogEntryAdded;
        public static event OnUserModerationLogModifiedDelegate OnUserNote;
        public static event OnUserModerationLogModifiedDelegate OnUserWarning;
        public static event OnUserModerationLogModifiedDelegate OnUserMuted;
        public static event OnUserModerationLogModifiedDelegate OnUserUnMuted;
        public static event OnUserModerationLogModifiedDelegate OnUserKicked;
        public static event OnUserModerationLogModifiedDelegate OnUserBanned;
        public static event OnUserModerationLogModifiedDelegate OnUserUnBanned;

        internal async Task InvokeUserModerationLogModifiedEvents(UserModerationLog userLog, UserModerationEntry entry)
        {
            await TryInvokeUserModerationLogModifiedEvent(OnUserModerationLogEntryAdded, userLog, entry);
            switch (entry.Type)
            {
                case ModerationType.Note:
                    await TryInvokeUserModerationLogModifiedEvent(OnUserNote, userLog, entry);
                    break;
                case ModerationType.Warning:
                    await TryInvokeUserModerationLogModifiedEvent(OnUserWarning, userLog, entry);
                    break;
                case ModerationType.Muted:
                    await TryInvokeUserModerationLogModifiedEvent(OnUserMuted, userLog, entry);
                    break;
                case ModerationType.UnMuted:
                    await TryInvokeUserModerationLogModifiedEvent(OnUserUnMuted, userLog, entry);
                    break;
                case ModerationType.Kicked:
                    await TryInvokeUserModerationLogModifiedEvent(OnUserKicked, userLog, entry);
                    break;
                case ModerationType.Banned:
                    await TryInvokeUserModerationLogModifiedEvent(OnUserBanned, userLog, entry);
                    break;
                case ModerationType.UnBanned:
                    await TryInvokeUserModerationLogModifiedEvent(OnUserUnBanned, userLog, entry);
                    break;
            }
        }

        private async Task TryInvokeUserModerationLogModifiedEvent(OnUserModerationLogModifiedDelegate eventDelegates, UserModerationLog userLog, UserModerationEntry entry)
        {
            if (eventDelegates != null)
            {
                foreach (Delegate del in eventDelegates.GetInvocationList())
                {
                    OnUserModerationLogModifiedDelegate deleg = del as OnUserModerationLogModifiedDelegate;
                    if (deleg != null)
                    {
                        try
                        {
                            await deleg(this, userLog, entry);
                        }
                        catch (Exception e)
                        {
                            await YNBBotCore.Logger(new LogMessage(LogSeverity.Error, "GML", "A user moderation log handler threw an unhandled exception", e));
                        }
                    }
                }
            }
        }



        public delegate Task OnChannelModeratedDelegate(GuildModerationLog guildLog, ChannelModerationEntry entry);

        public event OnChannelModeratedDelegate OnChannelLocked;
        public event OnChannelModeratedDelegate OnChannelUnlocked;
        public event OnChannelModeratedDelegate OnChannelPurged;

        public event OnChannelModeratedDelegate OnChannelModerated;

        private async Task InvokeChannelModeratedEvents(ChannelModerationEntry entry)
        {
            await TryInvokeChannelModeratedEvent(OnChannelModerated, entry);
            switch (entry.Type)
            {
                case ChannelModerationType.Locked:
                    await TryInvokeChannelModeratedEvent(OnChannelLocked, entry);
                    break;
                case ChannelModerationType.Unlocked:
                    await TryInvokeChannelModeratedEvent(OnChannelUnlocked, entry);
                    break;
                case ChannelModerationType.Purged:
                    await TryInvokeChannelModeratedEvent(OnChannelPurged, entry);
                    break;
            }
        }

        private async Task TryInvokeChannelModeratedEvent(OnChannelModeratedDelegate eventDelegates, ChannelModerationEntry entry)
        {
            if (eventDelegates != null)
            {
                foreach (Delegate del in eventDelegates.GetInvocationList())
                {
                    OnChannelModeratedDelegate deleg = del as OnChannelModeratedDelegate;
                    if (deleg != null)
                    {
                        try
                        {
                            await deleg(this, entry);
                        }
                        catch (Exception e)
                        {
                            await YNBBotCore.Logger(new LogMessage(LogSeverity.Error, "GML", "A channel moderated log handler threw an unhandled exception", e));
                        }
                    }
                }
            }
        }
    }
}
