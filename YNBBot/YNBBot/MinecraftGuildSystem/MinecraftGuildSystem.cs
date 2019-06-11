using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YNBBot.PagedStorageService;
using YNBBot.NestedCommands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;
using JSON;

namespace YNBBot.MinecraftGuildSystem
{
    static class MinecraftGuildModel
    {
        public static List<MinecraftGuild> Guilds = new List<MinecraftGuild>();

        public const int MIN_GUILDFOUNDINGMEMBERS = 1;
        private const int GUILD_ROLE_POSITION = 2;

        public static List<GuildColor> AvailableColors
        {
            get
            {
                List<GuildColor> allColors = new List<GuildColor>((GuildColor[])Enum.GetValues(typeof(GuildColor)));

                foreach (MinecraftGuild guild in Guilds)
                {
                    allColors.Remove(guild.Color);
                }

                return allColors;
            }
        }

        public static bool ColorIsAvailable(GuildColor color)
        {
            return AvailableColors.Contains(color);
        }

        public static bool NameIsAvailable(string name)
        {
            foreach (MinecraftGuild guild in Guilds)
            {
                if (guild.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool TryGetGuildOfUser(ulong id, out MinecraftGuild guild)
        {
            foreach (MinecraftGuild item in Guilds)
            {
                if (item.CaptainId == id || item.MemberIds.Contains(id))
                {
                    guild = item;
                    return true;
                }
            }
            guild = null;
            return false;
        }

        public static async Task Init()
        {
            var fileOperation = await ResourcesModel.LoadToJSONObject(ResourcesModel.GuildsFilePath);
            if (fileOperation.Success)
            {
                if (fileOperation.Result.IsArray)
                {
                    foreach (JSONField guild_json in fileOperation.Result.Array)
                    {
                        if (guild_json.IsObject)
                        {
                            MinecraftGuild guild = new MinecraftGuild();
                            if (guild.FromJSON(guild_json.Container))
                            {
                                Guilds.Add(guild);
                            }
                        }
                    }
                }
            }
        }

        public static async Task SaveAll()
        {
            JSONContainer json = JSONContainer.NewArray();
            foreach (MinecraftGuild guild in Guilds)
            {
                json.Add(guild.ToJSON());
            }
            await ResourcesModel.WriteJSONObjectToFile(ResourcesModel.GuildsFilePath, json);
        }

        private static readonly OverwritePermissions GuildRoleChannelPerms = new OverwritePermissions(addReactions:PermValue.Allow, viewChannel:PermValue.Allow, sendMessages:PermValue.Allow, readMessageHistory:PermValue.Allow);
        private static readonly OverwritePermissions CaptainChannelPerms = new OverwritePermissions(addReactions:PermValue.Allow, viewChannel:PermValue.Allow, sendMessages:PermValue.Allow, readMessageHistory:PermValue.Allow, manageMessages:PermValue.Allow);

        public static async Task<bool> CreateGuildAsync(SocketGuild guild, string name, GuildColor color, SocketGuildUser captain, List<SocketGuildUser> members)
        {
            string errorhint = "Failed to create Guild Role!";
            try
            {
                RestRole guildRole = await guild.CreateRoleAsync(name, color: new Discord.Color((uint)color), isHoisted:true);
                errorhint = "Move role into position";
                await guildRole.ModifyAsync(RoleProperties => 
                {
                    RoleProperties.Position = GUILD_ROLE_POSITION;
                });
                errorhint = "Failed to create Guild Channel!";
                SocketCategoryChannel guildCategory = guild.GetChannel(GuildChannelHelper.GuildCategoryId) as SocketCategoryChannel;
                if (guildCategory == null)
                {
                    throw new Exception("Could not find Guild Category Channel!");
                } 
                RestTextChannel guildChannel = await guild.CreateTextChannelAsync(name, TextChannelProperties =>
                {
                    TextChannelProperties.CategoryId = GuildChannelHelper.GuildCategoryId;
                    TextChannelProperties.Topic = "Private Guild Channel for " + name;
                });
                errorhint = "Failed to copy guildcategories permissions";
                foreach (Overwrite overwrite in guildCategory.PermissionOverwrites)
                {
                    IRole role = guild.GetRole(overwrite.TargetId);
                    if (role != null)
                    {
                        await guildChannel.AddPermissionOverwriteAsync(role, overwrite.Permissions);
                    }
                }
                errorhint = "Failed to set Guild Channel Permissions!";
                await guildChannel.AddPermissionOverwriteAsync(guildRole, GuildRoleChannelPerms);
                await guildChannel.AddPermissionOverwriteAsync(captain, CaptainChannelPerms);
                errorhint = "Failed to add Guild Role to Captain!";
                await captain.AddRoleAsync(guildRole);
                errorhint = "Failed to add Guild Role to a Member!";
                foreach (SocketGuildUser member in members)
                {
                    await member.AddRoleAsync(guildRole);
                }
                errorhint = "Failed to create MinecraftGuild!";
                MinecraftGuild minecraftGuild = new MinecraftGuild() { CaptainId = captain.Id, Name = name, Color = color, ChannelId = guildChannel.Id, RoleId = guildRole.Id };
                foreach (SocketGuildUser member in members)
                {
                    minecraftGuild.MemberIds.Add(member.Id);
                }
                Guilds.Add(minecraftGuild);
                errorhint = "Failed to save MinecraftGuild!";
                await SaveAll();

                return true;
            }
            catch (Exception e)
            {
                await GuildChannelHelper.SendExceptionNotification(e, $"Error creating guild {guild.Name}. Hint: {errorhint}");
                return false;
            }
        }

        public static async Task<bool> MemberJoinGuildAsync(MinecraftGuild guild, SocketGuildUser newMember)
        {
            string errorhint = "Adding Guild Role";
            try
            {
                if (Var.client.TryGetRole(guild.RoleId, out SocketRole guildRole) && !guild.MemberIds.Contains(newMember.Id))
                {
                    await newMember.AddRoleAsync(guildRole);
                    errorhint = "Adding Member to Guild and Saving";
                    guild.MemberIds.Add(newMember.Id);
                    await SaveAll();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                await GuildChannelHelper.SendExceptionNotification(e, $"Error joining player {newMember?.Mention} to guild {guild.Name}. Hint: {errorhint}");
                return false;
            }
        }

        public static async Task<bool> MemberLeaveGuildAsync(MinecraftGuild guild, SocketGuildUser leavingMember)
        {
            string errorhint = "Removing Guild Role";
            try
            {
                if (guild.MemberIds.Contains(leavingMember.Id))
                { 
                    foreach (SocketRole role in leavingMember.Roles)
                    {
                        if (role.Id == guild.RoleId)
                        {
                            await leavingMember.RemoveRoleAsync(role);
                            break;
                        }
                    }
                    errorhint = "Removing Member from Guild and Saving";
                    guild.MemberIds.Remove(leavingMember.Id);
                    await SaveAll();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                await GuildChannelHelper.SendExceptionNotification(e, $"Error leaving player {leavingMember?.Mention} from guild {guild.Name}. Hint: {errorhint}");
                return false;
            }
        }

        public static async Task<bool> UpdateGuildNameAsync(MinecraftGuild guild, string newName)
        {
            string errorhint = "Modifying Guild Channel";
            try
            {
                if (GuildChannelHelper.TryGetChannel(guild.ChannelId, out SocketTextChannel guildChannel) && Var.client.TryGetRole(guild.RoleId, out SocketRole guildRole))
                {
                    await guildChannel.ModifyAsync(GuildChannelProperties =>
                    {
                        GuildChannelProperties.Name = newName;
                        GuildChannelProperties.Topic = "Private Guild Channel for " + newName;
                    });
                    errorhint = "Modifying Guild Role";
                    await guildRole.ModifyAsync(RoleProperties =>
                    {
                        RoleProperties.Name = newName;
                    });
                    errorhint = "Setting Guild Name";
                    guild.Name = newName;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                await GuildChannelHelper.SendExceptionNotification(e, $"Error renaming guild {guild.Name} to {newName}. Hint: {errorhint}");
                return false;
            }
        }

        public static async Task<bool> UpdateGuildColorAsync(MinecraftGuild guild, GuildColor newColor)
        {
            string errorhint = "Modifying Guild Role";
            try
            {
                if (Var.client.TryGetRole(guild.RoleId, out SocketRole guildRole))
                {
                    await guildRole.ModifyAsync(RoleProperties =>
                    {
                        RoleProperties.Color = new Color((uint)newColor);
                    });
                    errorhint = "Setting Guild Name";
                    guild.Color = newColor;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                await GuildChannelHelper.SendExceptionNotification(e, $"Error recoloring guild {guild.Name} to {newColor}. Hint: {errorhint}");
                return false;
            }
        }

        public static async Task<bool> DeleteGuildAsync(MinecraftGuild guild)
        {
            string errorhint = "Removing Guild Role";
            try
            {
                if (Var.client.TryGetRole(guild.RoleId, out SocketRole guildRole))
                {
                    await guildRole.DeleteAsync();
                }
                errorhint = "Removing Guild Channel";
                if (GuildChannelHelper.TryGetChannel(guild.ChannelId, out SocketTextChannel guildChannel))
                {
                    await guildChannel.DeleteAsync();
                }
                errorhint = "Removing Guild and Saving";
                Guilds.Remove(guild);
                await SaveAll();
                return true;
            }
            catch (Exception e)
            {
                await GuildChannelHelper.SendExceptionNotification(e, $"Error removing guild {guild.Name}. Hint: {errorhint}");
                return false;
            }
        }
    }
}
