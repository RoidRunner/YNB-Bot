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
    /// <summary>
    /// Manages minecraft guilds
    /// </summary>
    static class MinecraftGuildModel
    {
        #region Fields and Properties

        private static List<MinecraftGuild> guilds = new List<MinecraftGuild>();

        /// <summary>
        /// Returns a list of all Guilds
        /// </summary>
        public static IReadOnlyList<MinecraftGuild> Guilds => guilds.AsReadOnly();

        /// <summary>
        /// Min amount of members required to found a guild
        /// </summary>
        public const int MIN_GUILDFOUNDINGMEMBERS = 2;
        private const int GUILD_ROLE_POSITION = 2;

        private static readonly OverwritePermissions GuildRoleChannelPerms = new OverwritePermissions(addReactions: PermValue.Allow, viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, readMessageHistory: PermValue.Allow);
        private static readonly OverwritePermissions CaptainChannelPerms = new OverwritePermissions(addReactions: PermValue.Allow, viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, readMessageHistory: PermValue.Allow, manageMessages: PermValue.Allow);

        private static readonly EmbedBuilder GuildHelpEmbed;

        /// <summary>
        /// List of all available guild colors.
        /// </summary>
        public static List<GuildColor> AvailableColors
        {
            get
            {
                List<GuildColor> allColors = new List<GuildColor>((GuildColor[])Enum.GetValues(typeof(GuildColor)));

                if (guilds.Count < 14)
                {

                    foreach (MinecraftGuild guild in guilds)
                    {
                        if (guild.NameAndColorFound)
                        {
                            allColors.Remove(guild.Color);
                        }
                    }
                }

                return allColors;
            }
        }

        #endregion
        #region CheckOperations

        /// <summary>
        /// Returns true if the color provided is still available
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static bool ColorIsAvailable(GuildColor color)
        {
            return AvailableColors.Contains(color);
        }

        /// <summary>
        /// Returns true if the name provided is still available
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool NameIsAvailable(string name)
        {
            foreach (MinecraftGuild guild in guilds)
            {
                if (guild.NameAndColorFound)
                {
                    if (guild.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static readonly List<char> legalChars = new List<char>(new char[] {
            '1', '2', '3', '4', '5', '5', '6', '7', '8', '9', '0',
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
            'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
            'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            '!', '\'', '#', '$', '%', '&', '(', ')', '*', '+', ',', '-', '_', '.',
            ':', ';', '<', '=', '>', '?', '@', '[', ']', '^', '`', '{', '}', '~', ' '
        });

        /// <summary>
        /// Checks a name for illegal characters and makes sure neither front or end are whitespace characters
        /// </summary>
        /// <param name="name">Name to check</param>
        /// <returns>true, if the name passed the test</returns>
        public static bool NameIsLegal(string name)
        {
            if (name[0] == ' ' || name[name.Length-1] == ' ')
            {
                return false;
            }
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (!legalChars.Contains(c))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Attempts to find a guild
        /// </summary>
        /// <param name="name">Name filter to select a guild</param>
        /// <param name="guild">Guild result</param>
        /// <param name="invalidDatasets">Wether to include guilds where name and color could not be sourced (Role not found)</param>
        /// <returns>True, if a result was found</returns>
        public static bool TryGetGuild(string name, out MinecraftGuild guild, bool invalidDatasets = false)
        {
            foreach (MinecraftGuild item in guilds)
            {
                item.TryFindNameAndColor();
                if (string.Equals(item.Name, name, StringComparison.InvariantCultureIgnoreCase))
                {
                    guild = item;
                    return guild.NameAndColorFound || invalidDatasets;
                }
            }
            guild = null;
            return false;
        }

        /// <summary>
        /// Attempts to find a guild
        /// </summary>
        /// <param name="id">Id of the user to check guild membership for</param>
        /// <param name="guild">Guild result</param>
        /// <param name="invalidDatasets">Wether to include guilds where name and color could not be sourced (Role not found)</param>
        /// <returns>True, if a result was found</returns>
        public static bool TryGetGuildOfUser(ulong id, out MinecraftGuild guild, bool invalidDatasets = false)
        {
            foreach (MinecraftGuild item in guilds)
            {
                if (item.CaptainId == id || item.MemberIds.Contains(id))
                {
                    guild = item;
                    return guild.NameAndColorFound || invalidDatasets;
                }
            }
            guild = null;
            return false;
        }

        #endregion
        #region Constructor

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

        #endregion
        #region Save/Load

        public static async Task Load()
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
                                guilds.Add(guild);
                            }
                        }
                    }
                }
            }
        }

        public static async Task SaveAll()
        {
            JSONContainer json = JSONContainer.NewArray();
            foreach (MinecraftGuild guild in guilds)
            {
                if (guild.NameAndColorFound)
                {
                    json.Add(guild.ToJSON());
                }
            }
            await ResourcesModel.WriteJSONObjectToFile(ResourcesModel.GuildsFilePath, json);
        }

        #endregion
        #region Creating/Modifying/Deleting Guilds

        /// <summary>
        /// Creates a new guild (includes role, channel, etc)
        /// </summary>
        /// <param name="guild">Discord Server Guild to create channel and role on</param>
        /// <param name="name">Guild Name</param>
        /// <param name="color">Guild Display Color</param>
        /// <param name="captain">Guild Captain</param>
        /// <param name="members">Guild Users</param>
        /// <returns>true, if operation succeeds</returns>
        public static async Task<bool> CreateGuildAsync(SocketGuild guild, string name, GuildColor color, SocketGuildUser captain, List<SocketGuildUser> members)
        {
            string errorhint = "Failed to create Guild Role!";
            try
            {
                RestRole guildRole = await guild.CreateRoleAsync(name, color:MinecraftGuild.ToDiscordColor(color), isHoisted: true);
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
                    SocketGuildUser member = members[i];
                    minecraftGuild.MemberIds.Add(member.Id);
                    memberPingString.Append(member.Mention);
                    if (i < members.Count - 1)
                    {
                        memberPingString.Append(", ");
                    }
                }
                guilds.Add(minecraftGuild);
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

        /// <summary>
        /// Adds a member to a guild
        /// </summary>
        /// <param name="guild">Guild the new member joins</param>
        /// <param name="newMember">New member that joins the guild</param>
        /// <returns>true, if operation succeeds</returns>
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
                await GuildChannelHelper.SendExceptionNotification(e, $"Error joining player {newMember?.Mention} to guild {guild?.Name}. Hint: {errorhint}");
                return false;
            }
        }

        /// <summary>
        /// Removes a member from a guild
        /// </summary>
        /// <param name="guild">Guild the member leaves</param>
        /// <param name="leavingMember">Member that leaves</param>
        /// <returns>true, if operation succeeds</returns>
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

        /// <summary>
        /// Changes a guilds name
        /// </summary>
        /// <param name="guild">Guild to modify</param>
        /// <param name="newName">New name for the guild</param>
        /// <returns>true, if operation succeeds</returns>
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

        /// <summary>
        /// Changes a guilds color
        /// </summary>
        /// <param name="guild">Guild to update</param>
        /// <param name="newColor">New color to apply</param>
        /// <returns>true, if operation succeeds</returns>
        public static async Task<bool> UpdateGuildColorAsync(MinecraftGuild guild, GuildColor newColor)
        {
            string errorhint = "Modifying Guild Role";
            try
            {
                if (Var.client.TryGetRole(guild.RoleId, out SocketRole guildRole))
                {
                    await guildRole.ModifyAsync(RoleProperties =>
                    {
                        RoleProperties.Color = MinecraftGuild.ToDiscordColor(newColor);
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

        /// <summary>
        /// Appoints a new guild captain to a guild
        /// </summary>
        /// <param name="guild">Guild to set the captain for</param>
        /// <param name="newCaptain">New captain to appoint to the guild</param>
        /// <param name="oldCaptain">Old captain (can be null)</param>
        /// <returns>true, if operation succeeds</returns>
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

        /// <summary>
        /// Deletes a guild both on server and in data
        /// </summary>
        /// <param name="guild">Guild to remove</param>
        /// <returns>True, if operation completed</returns>
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

        /// <summary>
        /// Removes the guild represantation in the guilds list and saves
        /// </summary>
        /// <param name="guild">Guild to remove</param>
        public static async Task DeleteGuildDatasetAsync(MinecraftGuild guild)
        {
            guilds.Remove(guild);
            await SaveAll();
        }

        #endregion
    }
}
