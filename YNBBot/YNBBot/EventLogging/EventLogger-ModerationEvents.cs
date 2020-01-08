using BotCoreNET;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YNBBot.Moderation;

namespace YNBBot.EventLogging
{
    internal static partial class EventLogger
    {
        public static void SubscribeToModerationEvents()
        {
            GuildModerationLog.OnUserBanned += GuildModerationLog_OnUserBanned;
            GuildModerationLog.OnUserUnBanned += GuildModerationLog_OnUserUnBanned;
            GuildModerationLog.OnUserMuted += GuildModerationLog_OnUserMuted;
            GuildModerationLog.OnUserUnMuted += GuildModerationLog_OnUserUnMuted;
            GuildModerationLog.OnUserKicked += GuildModerationLog_OnUserKicked;
            GuildModerationLog.OnUserWarning += GuildModerationLog_OnUserWarning;
            GuildModerationLog.OnUserNote += GuildModerationLog_OnUserNote;
        }

        private static Task GuildModerationLog_OnUserNote(GuildModerationLog guildLog, UserModerationLog userLog, UserModerationEntry entry)
        {
            SocketGuild guild = BotCore.Client.GetGuild(guildLog.GuildId);
            if ((guild != null) && TryGetLogChannel(guild, ModerationType.Note, out SocketTextChannel logChannel, out EmbedBuilder embed))
            {
                embed.Title = $"Note added for {getUserString(userLog.UserId, guild)}";
                embed.Description =
                    $"Actor: {getUserString(entry.ActorId, guild)}\n" +
                    $"Note: {entry.Reason}";
                return logChannel.SendEmbedAsync(embed);
            }
            return Task.CompletedTask;
        }

        private static string getUserString(ulong userId, SocketGuild guild)
        {
            string userString;
            SocketGuildUser user = guild.GetUser(userId);
            if (user != null)
            {
                userString = $"{user} ({userId})";
            }
            else
            {
                userString = userId.ToString();
            }

            return userString;
        }

        private static Task GuildModerationLog_OnUserWarning(GuildModerationLog guildLog, UserModerationLog userLog, UserModerationEntry entry)
        {
            SocketGuild guild = BotCore.Client.GetGuild(guildLog.GuildId);
            if ((guild != null) && TryGetLogChannel(guild, ModerationType.Note, out SocketTextChannel logChannel, out EmbedBuilder embed))
            {
                embed.Title = $"Warning added for {getUserString(userLog.UserId, guild)}";
                embed.Description =
                    $"Actor: {getUserString(entry.ActorId, guild)}\n" +
                    $"Warning: `{entry.Reason}`";
                return logChannel.SendEmbedAsync(embed);
            }
            return Task.CompletedTask;
        }

        private static Task GuildModerationLog_OnUserKicked(GuildModerationLog guildLog, UserModerationLog userLog, UserModerationEntry entry)
        {
            SocketGuild guild = BotCore.Client.GetGuild(guildLog.GuildId);
            if ((guild != null) && TryGetLogChannel(guild, ModerationType.Note, out SocketTextChannel logChannel, out EmbedBuilder embed))
            {
                embed.Title = $"Kicked User {getUserString(userLog.UserId, guild)}";
                embed.Description =
                    $"Actor: {getUserString(entry.ActorId, guild)}\n" +
                    $"Reason: {entry.Reason}";
                return logChannel.SendEmbedAsync(embed);
            }
            return Task.CompletedTask;
        }

        private static Task GuildModerationLog_OnUserUnMuted(GuildModerationLog guildLog, UserModerationLog userLog, UserModerationEntry entry)
        {
            SocketGuild guild = BotCore.Client.GetGuild(guildLog.GuildId);
            if ((guild != null) && TryGetLogChannel(guild, ModerationType.Note, out SocketTextChannel logChannel, out EmbedBuilder embed))
            {
                embed.Title = $"Unmuted User {getUserString(userLog.UserId, guild)}";
                embed.Description =
                    $"Actor: {getUserString(entry.ActorId, guild)}\n" +
                    $"Reason: {entry.Reason}";
                return logChannel.SendEmbedAsync(embed);
            }
            return Task.CompletedTask;
        }

        private static Task GuildModerationLog_OnUserMuted(GuildModerationLog guildLog, UserModerationLog userLog, UserModerationEntry entry)
        {
            SocketGuild guild = BotCore.Client.GetGuild(guildLog.GuildId);
            if ((guild != null) && TryGetLogChannel(guild, ModerationType.Note, out SocketTextChannel logChannel, out EmbedBuilder embed))
            {
                embed.Title = $"Muted User {getUserString(userLog.UserId, guild)}";
                embed.Description =
                    $"Actor: {getUserString(entry.ActorId, guild)}\n" +
                    (string.IsNullOrEmpty(entry.Info) ? "" : $"Info: {entry.Info}\n") +
                    $"Reason: {entry.Reason}";
                return logChannel.SendEmbedAsync(embed);
            }
            return Task.CompletedTask;
        }

        private static Task GuildModerationLog_OnUserUnBanned(GuildModerationLog guildLog, UserModerationLog userLog, UserModerationEntry entry)
        {
            SocketGuild guild = BotCore.Client.GetGuild(guildLog.GuildId);
            if ((guild != null) && TryGetLogChannel(guild, ModerationType.Note, out SocketTextChannel logChannel, out EmbedBuilder embed))
            {
                embed.Title = $"Unbanned User {getUserString(userLog.UserId, guild)}";
                embed.Description =
                    $"Actor: {getUserString(entry.ActorId, guild)}\n" +
                    $"Reason: {entry.Reason}";
                return logChannel.SendEmbedAsync(embed);
            }
            return Task.CompletedTask;
        }

        private static Task GuildModerationLog_OnUserBanned(GuildModerationLog guildLog, UserModerationLog userLog, UserModerationEntry entry)
        {
            SocketGuild guild = BotCore.Client.GetGuild(guildLog.GuildId);
            if ((guild != null) && TryGetLogChannel(guild, ModerationType.Note, out SocketTextChannel logChannel, out EmbedBuilder embed))
            {
                embed.Title = $"Banned User {getUserString(userLog.UserId, guild)}";
                embed.Description =
                    $"Actor: {getUserString(entry.ActorId, guild)}\n" +
                    (string.IsNullOrEmpty(entry.Info) ? "" : $"Info: {entry.Info}\n") +
                    $"Reason: {entry.Reason}";
                return logChannel.SendEmbedAsync(embed);
            }
            return Task.CompletedTask;
        }

        private static bool TryGetLogChannel(SocketGuild guild, ModerationType type, out SocketTextChannel channel, out EmbedBuilder baseEmbed)
        {
            if (GuildEventLoggerSettings.TryGetValue(guild.Id, out EventLoggerSettings settings))
            {
                if (settings.UserModLogChannels.TryGetValue(type, out ulong channelId))
                {
                    channel = guild.GetTextChannel(channelId);
                    baseEmbed = GetBaseEmbed(type);
                    return channel != null;
                }
            }
            channel = default;
            baseEmbed = null;
            return false;
        }

        private static EmbedBuilder GetBaseEmbed(ModerationType type)
        {
            return new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = type.ToString()
                },
                Timestamp = DateTimeOffset.UtcNow,
                Color = BotCore.EmbedColor
            };
        }
    }
}
