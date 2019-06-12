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
using YNBBot.Interactive;

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

                if (Guilds.Count < 14)
                {

                    foreach (MinecraftGuild guild in Guilds)
                    {
                        if (guild.TryRetrieveNameAndColor())
                        {
                            allColors.Remove(guild.Color);
                        }
                    }
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
                if (guild.TryRetrieveNameAndColor())
                {
                    if (guild.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool GetGuild(string name, out MinecraftGuild guild, bool invalidDatasets = false)
        {
            foreach (MinecraftGuild item in Guilds)
            {
                item.TryRetrieveNameAndColor();
                if (string.Equals(item.Name, name, StringComparison.InvariantCultureIgnoreCase))
                {
                    guild = item;
                    return guild.TryRetrieveNameAndColor() || invalidDatasets;
                }
            }
            guild = null;
            return false;
        }

        public static bool TryGetGuildOfUser(ulong id, out MinecraftGuild guild, bool invalidDatasets = false)
        {
            foreach (MinecraftGuild item in Guilds)
            {
                if (item.CaptainId == id || item.MemberIds.Contains(id))
                {
                    guild = item;
                    return guild.TryRetrieveNameAndColor() || invalidDatasets;
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

        public static readonly EmbedBuilder GuildHelpEmbed;

        static MinecraftGuildModel()
        {
            GuildHelpEmbed = new EmbedBuilder()
            {
                Title = "Information on Guilds",
                Color = Var.BOTCOLOR,
                Description = "Use `/help guild` for an overview of all guild related commands!\n\nGuild members are managed by the guild captain. They can invite and kick members. " +
                "Leaving and joining guilds happens instantly on discord, but is manually done ingame by admins, which are automatically notified of the changes.\n" +
                "Any member can leave the guild as they please with `/guild leave`. The guild captain can not leave a guild but delete it with the same command, assuming no other members are left.\n\n" +
                "If you encounter any problems tell a <@&554485497192513540> or the bot programmer, <@117260771200598019>"
            };
            GuildHelpEmbed.AddField("Member commands", "`/guild leave` - Leave this guild");
            GuildHelpEmbed.AddField("Captain commands", "`/guild invite [<Member>]` - Invite members to join your guild\n" +
                "`/guild kick [<Member>]` - Kick members from your guild\n" +
                "`/guild passcaptain <Member>` - Pass your captain rights to another user\n" +
                "`/guild leave` - Leave this guild (deleting it), after all other members have left");
        }

        public static async Task SaveAll()
        {
            JSONContainer json = JSONContainer.NewArray();
            foreach (MinecraftGuild guild in Guilds)
            {
                if (guild.TryRetrieveNameAndColor())
                {
                    json.Add(guild.ToJSON());
                }
            }
            await ResourcesModel.WriteJSONObjectToFile(ResourcesModel.GuildsFilePath, json);
        }

        private static readonly OverwritePermissions GuildRoleChannelPerms = new OverwritePermissions(addReactions: PermValue.Allow, viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, readMessageHistory: PermValue.Allow);
        private static readonly OverwritePermissions CaptainChannelPerms = new OverwritePermissions(addReactions: PermValue.Allow, viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, readMessageHistory: PermValue.Allow, manageMessages: PermValue.Allow);

        public static async Task<bool> CreateGuildAsync(SocketGuild guild, string name, GuildColor color, SocketGuildUser captain, List<SocketGuildUser> members)
        {
            string errorhint = "Failed to create Guild Role!";
            try
            {
                RestRole guildRole = await guild.CreateRoleAsync(name, color: new Discord.Color((uint)color), isHoisted: true);
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

                StringBuilder memberPingString = new StringBuilder();

                MinecraftGuild minecraftGuild = new MinecraftGuild(guildChannel.Id, guildRole.Id, color, name, captain.Id);
                for (int i = 0; i < members.Count; i++)
                {
                    SocketGuildUser member = (SocketGuildUser)members[i];
                    minecraftGuild.MemberIds.Add(member.Id);
                    memberPingString.Append(member.Mention);
                    if (i < members.Count - 1)
                    {
                        memberPingString.Append(", ");
                    }
                }
                Guilds.Add(minecraftGuild);
                errorhint = "Failed to save MinecraftGuild!";
                await SaveAll();
                errorhint = "Failed to send or pin guild info embed";
                var infomessage = await guildChannel.SendMessageAsync(embed: GuildHelpEmbed.Build());
                await infomessage.PinAsync();
                errorhint = "Notify Admins";
                await AdminTaskInteractiveMessage.CreateAdminTaskMessage($"Create ingame represantation for guild \"{name}\"", $"Name: `{name}`, Color: `{color}` (`0x{((uint)color).ToString("X")}`)\nCaptain: {captain.Mention}\nMembers: {memberPingString}");
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
                    errorhint = "Notify Admins";
                    await AdminTaskInteractiveMessage.CreateAdminTaskMessage($"Add user \"{newMember}\" to guild \"{guild.Name}\" ingame", "Joining User: " + newMember.Mention);
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
                    errorhint = "Notify Admins";
                    await AdminTaskInteractiveMessage.CreateAdminTaskMessage($"Remove user \"{leavingMember}\" from guild \"{guild.Name}\" ingame", "Leaving User: " + leavingMember.Mention);
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
                    string oldname = guild.Name;
                    guild.Name = newName;
                    errorhint = "Notify Admins";
                    await AdminTaskInteractiveMessage.CreateAdminTaskMessage($"Rename guild \"{oldname}\" to \"{newName}\" ingame", string.Empty);
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
                    errorhint = "Setting Guild Color";
                    guild.Color = newColor;
                    errorhint = "Notify Admins";
                    await AdminTaskInteractiveMessage.CreateAdminTaskMessage($"Change color of guild \"{guild.Name}\" to \"{newColor}\" ingame", $"Color: `{ newColor}` (`0x{ ((uint)newColor).ToString("X")}`)");
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

        public static async Task<bool> SetGuildCaptain(MinecraftGuild guild, SocketGuildUser newCaptain, SocketGuildUser oldCaptain)
        {
            string errorhint = "Modify channel perms";
            try
            {
                if (GuildChannelHelper.TryGetChannel(guild.ChannelId, out SocketTextChannel guildChannel))
                {
                    if (oldCaptain != null)
                    {
                        await guildChannel.RemovePermissionOverwriteAsync(oldCaptain);
                    }
                    await guildChannel.AddPermissionOverwriteAsync(newCaptain, CaptainChannelPerms);
                }
                errorhint = "Modify and save";
                if (oldCaptain != null)
                {
                    guild.MemberIds.Add(guild.CaptainId);
                }
                guild.MemberIds.Remove(newCaptain.Id);
                guild.CaptainId = newCaptain.Id;
                await SaveAll();
                return true;
            }
            catch (Exception e)
            {
                await GuildChannelHelper.SendExceptionNotification(e, $"Error setting captain for {guild.Name} to {newCaptain.Mention}. Hint: {errorhint}");
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
                await DeleteGuildDatasetAsync(guild);
                errorhint = "Notify Admins";
                await AdminTaskInteractiveMessage.CreateAdminTaskMessage($"Remove ingame represantation of guild\"{guild.Name}\"", string.Empty);
                return true;
            }
            catch (Exception e)
            {
                await GuildChannelHelper.SendExceptionNotification(e, $"Error removing guild {guild.Name}. Hint: {errorhint}");
                return false;
            }
        }

        public static async Task DeleteGuildDatasetAsync(MinecraftGuild guild)
        {
            Guilds.Remove(guild);
            await SaveAll();
        }
    }
}
