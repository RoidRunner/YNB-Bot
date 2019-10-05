using BotCoreNET;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.Moderation
{
    class TimeLimitedInfraction
    {
        public readonly DateTimeOffset Ends;
        public readonly ulong UserId;
        public readonly ulong GuildId;
        public readonly ModerationType Type;

        public TimeLimitedInfraction(DateTimeOffset ends, ulong userId, ulong guildId, ModerationType type)
        {
            Ends = ends;
            UserId = userId;
            GuildId = guildId;
            Type = type;
        }

        public bool TimeOver { get { return DateTimeOffset.UtcNow > Ends; } }
        public TimeSpan TimeLeft { get { return Ends - DateTimeOffset.UtcNow; } }

        public async Task<string> RelieveInfraction()
        {
            SocketGuild guild = BotCore.Client.GetGuild(GuildId);
            if (guild != null)
            {
                SocketGuildUser user = guild.GetUser(UserId);

                GuildModerationLog guildLog = GuildModerationLog.GetOrCreateGuildModerationLog(GuildId);
                UserModerationLog userLog = guildLog.GetOrCreateUserModerationLog(UserId);

                if (Type == ModerationType.Banned)
                {

                    if (userLog.BannedUntil.HasValue)
                    {
                        if (userLog.BannedUntil.Value == Ends)
                        {
                            await guild.RemoveBanAsync(UserId);
                            return null;
                        }
                    }
                    return "Banstate Invalid!";
                }

                if (Type == ModerationType.Muted)
                {

                }

                return $"Unhandled timelimited moderation type `{Type}`";
            }
            else
            {
                return $"Guild `{GuildId}` not found!";
            }
        }
    }
}
