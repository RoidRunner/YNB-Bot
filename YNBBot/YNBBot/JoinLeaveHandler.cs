using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YNBBot.Interactive;
using YNBBot.MinecraftGuildSystem;

namespace YNBBot
{
    static class JoinLeaveHandler
    {
        public static List<ulong> AutoAssignRoleIds = new List<ulong>();

        public static async Task HandleUserJoined(SocketGuildUser user)
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
                await user.AddRolesAsync(AssignRoles);
            }
            catch (Exception)
            {
            }
            await SettingsModel.SendDebugMessage(DebugCategories.joinleave, $"{user} joined {user.Guild}", $"Id: `{user.Id}`");
            await SettingsModel.WelcomeNewUser(user);
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
