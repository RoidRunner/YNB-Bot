using BotCoreNET;
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
            if (Directory.Exists(guildModerationLog.Path))
            {
                foreach (string filepath in Directory.EnumerateFiles(guildModerationLog.Path, "*.json"))
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
            GuildLogs.Add(guildModerationLog.GuildId, guildModerationLog);
        }

        #endregion
        #region UpdateThread

        private static Thread updateThread;
        private static object addlistlock = new object();
        private static object removelistlock = new object();
        private static List<UserModerationLog> timeLimitedInfractions = new List<UserModerationLog>();
        private static List<UserModerationLog> toBeAdded = new List<UserModerationLog>();
        private static List<UserModerationLog> toBeRemoved = new List<UserModerationLog>();

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
                                lock (removelistlock)
                                {
                                    toBeRemoved.Add(userModlog);
                                }
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

        #endregion
        #endregion

        private Dictionary<ulong, UserModerationLog> userLogs = new Dictionary<ulong, UserModerationLog>();

        public readonly ulong GuildId;
        public readonly string Path;

        private Dictionary<ModerationType, ulong> logChannels = new Dictionary<ModerationType, ulong>();

        public GuildModerationLog(ulong guildId)
        {
            GuildId = guildId;
            Path = $"{ResourcesModel.ModerationLogsPath}/{guildId}";
            AssurePath();
        }

        public void AssurePath()
        {
            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }
        }

        public UserModerationLog GetOrCreateUserModerationLog(ulong userId)
        {
            if (userLogs.TryGetValue(userId, out UserModerationLog usermoderationLog))
            {
                return usermoderationLog;
            }
            else
            {
                usermoderationLog = new UserModerationLog(this, userId);
                userLogs.Add(userId, usermoderationLog);
                return usermoderationLog;
            }
        }
    }
}
