using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YNBBot.Interactive;
using YNBBot.MinecraftGuildSystem;

namespace YNBBot.EventLogging
{
    static class EventLogger
    {
        public static void Subscribe(DiscordSocketClient client)
        {
            client.ChannelCreated += Client_ChannelCreated;
            client.ChannelDestroyed += Client_ChannelDestroyed;
            client.ChannelUpdated += Client_ChannelUpdated;

            client.GuildMemberUpdated += Client_GuildMemberUpdated;
            client.GuildUpdated += Client_GuildUpdated;

            client.MessageDeleted += Client_MessageDeleted;
            client.MessagesBulkDeleted += Client_MessagesBulkDeleted;
            client.MessageUpdated += Client_MessageUpdated;

            client.RoleCreated += Client_RoleCreated;
            client.RoleDeleted += Client_RoleDeleted;
            client.RoleUpdated += Client_RoleUpdated;

            client.UserJoined += Client_UserJoined;
            client.UserLeft += Client_UserLeft;
            client.UserBanned += Client_UserBanned;
            client.UserUnbanned += Client_UserUnbanned;

            client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
        }

        #region voice

        private static Task Client_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region user

        private static Task Client_UserUnbanned(SocketUser arg1, SocketGuild arg2)
        {
            throw new NotImplementedException();
        }

        private static Task Client_UserBanned(SocketUser arg1, SocketGuild arg2)
        {
            throw new NotImplementedException();
        }

        private static Task Client_UserLeft(SocketGuildUser arg)
        {
            throw new NotImplementedException();
        }

        private static async Task Client_UserJoined(SocketGuildUser arg)
        {
            await AssignAutoRoles(arg);
            await WelcomeUser(arg);
        }

        private static Task Client_GuildMemberUpdated(SocketGuildUser arg1, SocketGuildUser arg2)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region roles

        private static Task Client_RoleUpdated(SocketRole arg1, SocketRole arg2)
        {
            throw new NotImplementedException();
        }

        private static Task Client_RoleDeleted(SocketRole arg)
        {
            throw new NotImplementedException();
        }

        private static Task Client_RoleCreated(SocketRole arg)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region messages

        private static Task Client_MessageUpdated(Discord.Cacheable<Discord.IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            throw new NotImplementedException();
        }

        private static Task Client_MessagesBulkDeleted(IReadOnlyCollection<Discord.Cacheable<Discord.IMessage, ulong>> arg1, ISocketMessageChannel arg2)
        {
            throw new NotImplementedException();
        }

        private static Task Client_MessageDeleted(Discord.Cacheable<Discord.IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region guild

        private static Task Client_GuildUpdated(SocketGuild arg1, SocketGuild arg2)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region channels

        private static Task Client_ChannelUpdated(SocketChannel arg1, SocketChannel arg2)
        {
            throw new NotImplementedException();
        }

        private static Task Client_ChannelDestroyed(SocketChannel arg)
        {
            throw new NotImplementedException();
        }

        private static Task Client_ChannelCreated(SocketChannel arg)
        {
            throw new NotImplementedException();
        }

        #endregion

        public static List<ulong> AutoAssignRoleIds = new List<ulong>();

        public static async Task WelcomeUser(SocketGuildUser user)
        {
            await SettingsModel.SendDebugMessage(DebugCategories.joinleave, $"{user} joined {user.Guild}", $"Id: `{user.Id}`");
            await SettingsModel.WelcomeNewUser(user);
        }

        public static Task AssignAutoRoles(SocketGuildUser user)
        {
            try
            {
                List<SocketRole> AssignRoles = new List<SocketRole>();
                foreach (SocketRole role in user.Guild.Roles)
                {
                    if (AutoAssignRoleIds.Contains(role.Id))
                    {
                        AssignRoles.Add(role);
                    }
                }
                for (int i = 0; i < AssignRoles.Count; i++)
                {
                    SocketRole assignRole = AssignRoles[i];
                    if (user.Roles.Any((SocketRole hasRole) => { return hasRole.Id == assignRole.Id; }))
                    {
                        AssignRoles.RemoveAt(i);
                        i--;
                    }
                }
                return user.AddRolesAsync(AssignRoles);
            }
            catch (Exception)
            {
            }
            return Task.CompletedTask;
        }

        public static async Task HandleUserLeft(SocketGuildUser user)
        {
            if (MinecraftGuildModel.TryGetGuildOfUser(user.Id, out MinecraftGuild userGuild))
            {
                GuildRank rank = userGuild.GetMemberRank(user.Id);
                if (rank == GuildRank.Captain)
                {
                    await AdminTaskInteractiveMessage.CreateAdminTaskMessage($"Guild Captain left Server", $"Guild: `{userGuild.Name}`\nCaptain: `{user}`, Id: `{user.Id}`{(string.IsNullOrEmpty(user.Nickname) ? "" : $", Nickname: `{user.Nickname}`")}");
                }
                else
                {
                    userGuild.MemberIds.Remove(user.Id);
                    userGuild.MateIds.Remove(user.Id);
                    await MinecraftGuildModel.SaveAll();
                }
            }
            if (user.Roles.Any(role => { return role.Id == SettingsModel.MinecraftBranchRole; }))
            {
                await AdminTaskInteractiveMessage.CreateAdminTaskMessage($"Minecraft Branch Member left Server", $"Name: `{user}`, Id: `{user.Id}`{(string.IsNullOrEmpty(user.Nickname) ? "" : $", Nickname: `{user.Nickname}`")}");
            }
            await SettingsModel.SendDebugMessage(DebugCategories.joinleave, $"{user} left {user.Guild}", $"Id: `{user.Id}`, Nickname: `{(string.IsNullOrEmpty(user.Nickname) ? "None" : user.Nickname)}`");
        }
    }
}
