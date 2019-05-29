using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YNBBot.PagedStorageService;
using YNBBot.NestedCommands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;

namespace YNBBot.GuildSystem
{
    static class MinecraftGuildSystem
    {
        public static PagedStorageService<MinecraftGuild> Guilds = new PagedStorageService<MinecraftGuild>(ResourcesModel.GuildsDirectory);

        public static async Task Init()
        {
            await Guilds.InitialLoad();
        }

        private static readonly OverwritePermissions GuildRoleChannelPerms = new OverwritePermissions(addReactions:PermValue.Allow, viewChannel:PermValue.Allow, sendMessages:PermValue.Allow, readMessageHistory:PermValue.Allow);

        public static async Task CreateGuild(SocketGuild guild, string name, GuildColor color, SocketGuildUser captain, List<SocketGuildUser> members)
        {
            try
            {
                RestRole guildRole = await guild.CreateRoleAsync(name, color: new Discord.Color((uint)color));
                RestTextChannel guildChannel = await guild.CreateTextChannelAsync(name, TextChannelProperties =>
                {
                    TextChannelProperties.Topic = "Private Guild Channel for " + name;
                });
                await guildChannel.AddPermissionOverwriteAsync(guildRole, GuildRoleChannelPerms);
                await captain.AddRoleAsync(guildRole);
                foreach (SocketGuildUser member in members)
                {
                    await member.AddRoleAsync(guildRole);
                }
            }
            catch (Exception e)
            {

            }
        }
    }
}
