using BotCoreNET;
using BotCoreNET.BotVars;
using BotCoreNET.Helpers;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YNBBot.Interactive;
using YNBBot.MinecraftGuildSystem;
using YNBBot.Moderation;

namespace YNBBot.EventLogging
{
    static partial class EventLogger
    {
        public static Dictionary<ulong, EventLoggerSettings> GuildEventLoggerSettings = new Dictionary<ulong, EventLoggerSettings>();

        public static void OnBotVarUpdatedGuild(ulong guildId, BotVar botVar)
        {
            if (botVar.TryConvert(out EventLoggerSettings newSettings))
            {
                GuildEventLoggerSettings[guildId] = newSettings;
            }
        }

        public static void SubscribeToDiscordEvents(DiscordSocketClient client)
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

        internal static void SubscribeToBotVarCollection(BotVarCollection botVarCollection)
        {
            if (botVarCollection != BotVarManager.GlobalBotVars)
            {
                botVarCollection.SubscribeToBotVarUpdateEvent(OnBotVarUpdatedGuild, "logChannels");
                if (botVarCollection.TryGetBotVar("logChannels", out EventLoggerSettings settings))
                {
                    GuildEventLoggerSettings[botVarCollection.GuildID] = settings;
                }
            }
        }

        #region voice

        private static Task Client_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState old, SocketVoiceState updated)
        {
            SocketGuildUser user = arg1 as SocketGuildUser;
            if (user != null)
            {
                if (TryGetLogChannel(user.Guild, DiscordEventType.UserVoiceStatusUpdated, out SocketTextChannel channel, out EmbedBuilder embed))
                {
                    StringBuilder description = new StringBuilder();
                    if (old.VoiceChannel == null && updated.VoiceChannel != null)
                    {
                        description.AppendLine($"Joined {updated.VoiceChannel}");
                    }
                    else if (old.VoiceChannel != null && updated.VoiceChannel == null)
                    {
                        description.AppendLine($"Left {old.VoiceChannel}");
                    }
                    else if (old.VoiceChannel != null && old.VoiceChannel != updated.VoiceChannel)
                    {
                        description.AppendLine($"Moved from {old.VoiceChannel} to {updated.VoiceChannel}");
                    }
                    if (description.Length > 0)
                    {
                        embed.Title = $"{arg1} Voice Update";
                        embed.Description = description.ToString();
                        return channel.SendEmbedAsync(embed);
                    }
                }
            }
            return Task.CompletedTask;
        }

        #endregion
        #region user

        private static Task Client_UserUnbanned(SocketUser arg1, SocketGuild arg2)
        {
            if (TryGetLogChannel(arg2, DiscordEventType.UserUnbanned, out SocketTextChannel channel, out EmbedBuilder embed))
            {
                embed.Title = $"{arg1} was unbanned";
                return channel.SendEmbedAsync(embed);
            }
            return Task.CompletedTask;
        }

        private static Task Client_UserBanned(SocketUser arg1, SocketGuild arg2)
        {
            if (TryGetLogChannel(arg2, DiscordEventType.UserBanned, out SocketTextChannel channel, out EmbedBuilder embed))
            {
                embed.Title = $"{arg1} was banned";
                return channel.SendEmbedAsync(embed);
            }
            return Task.CompletedTask;
        }

        private static async Task Client_UserLeft(SocketGuildUser user)
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
            BotVarCollection guildBotVars = BotVarManager.GetGuildBotVarCollection(user.Guild.Id);
            if (guildBotVars.TryGetBotVar(Var.MinecraftBranchRoleBotVarId, out ulong minecraftBranchRole))
            {
                if (user.Roles.Any(role => { return role.Id == minecraftBranchRole; }))
                {
                    await AdminTaskInteractiveMessage.CreateAdminTaskMessage($"Minecraft Branch Member left Server", $"Name: `{user}`, Id: `{user.Id}`{(string.IsNullOrEmpty(user.Nickname) ? "" : $", Nickname: `{user.Nickname}`")}");
                }
            }

            if (TryGetLogChannel(user.Guild, DiscordEventType.UserLeft, out SocketTextChannel channel, out EmbedBuilder embed))
            {
                embed.Title = $"{user} left";
                StringBuilder description = new StringBuilder();
                if (!string.IsNullOrEmpty(user.Nickname))
                {
                    description.AppendLine($"**Nickname**: \"{user.Nickname}\"");
                }
                if (user.Roles.Count > 1)
                {
                    description.AppendLine($"**Roles**: `{string.Join(", ", user.Roles)}`");
                }
                embed.ThumbnailUrl = user.GetDefaultAvatarUrl();
                embed.Description = description.ToString();
                await channel.SendEmbedAsync(embed);
            }
        }

        private static async Task Client_UserJoined(SocketGuildUser user)
        {
            await AssignAutoRoles(user);
            await WelcomeUser(user);
            if (TryGetLogChannel(user.Guild, DiscordEventType.UserJoined, out SocketTextChannel channel, out EmbedBuilder embed))
            {
                embed.Title = $"{user} joined";
                await channel.SendEmbedAsync(embed);
            }

        }

        private static Task Client_GuildMemberUpdated(SocketGuildUser old, SocketGuildUser updated)
        {
            if (TryGetLogChannel(old.Guild, DiscordEventType.GuildMemberUpdated, out SocketTextChannel logChannel, out EmbedBuilder embed))
            {
                StringBuilder differences = new StringBuilder();
                if (old.Username != updated.Username)
                {
                    differences.AppendLine($"Changed username to \"{updated}\"");
                }
                if (old.Nickname != updated.Nickname)
                {
                    if (string.IsNullOrEmpty(updated.Nickname))
                    {
                        differences.AppendLine($"Reset nickname");
                    }
                    else if (string.IsNullOrEmpty(old.Nickname))
                    {
                        differences.AppendLine($"Set nickname to \"{updated.Nickname}\"");
                    }
                    else
                    {
                        differences.AppendLine($"Changed nickname from \"{old.Nickname}\" to \"{updated.Nickname}\"");
                    }
                }
                foreach (var role in old.Roles)
                {
                    if (!updated.Roles.Contains(role))
                    {
                        differences.AppendLine($"Removed role {role.Mention}");
                    }
                }
                foreach (var role in updated.Roles)
                {
                    if (!old.Roles.Contains(role))
                    {
                        differences.AppendLine($"Added role {role.Mention}");
                    }
                }
                if (differences.Length > 0)
                {
                    embed.Title = updated.ToString();
                    embed.Description = differences.ToString();
                    return logChannel.SendEmbedAsync(embed);
                }
            }
            return Task.CompletedTask;
        }

        #endregion
        #region roles

        private static Task Client_RoleUpdated(SocketRole old, SocketRole updated)
        {
            if (TryGetLogChannel(old.Guild, DiscordEventType.RoleUpdated, out SocketTextChannel channel, out EmbedBuilder embed))
            {
                StringBuilder description = new StringBuilder();
                if (old.Name != updated.Name)
                {
                    description.AppendLine($"Renamed from \"{old}\" to \"{updated}\"");
                }
                if (old.Color != updated.Color)
                {
                    description.AppendLine($"Changed Color from `{old.Color.RawValue.ToString("x")}` to `{updated.Color.RawValue.ToString("x")}`");
                }
                if (old.Permissions.RawValue != updated.Permissions.RawValue)
                {
                    var oldPerms = old.Permissions.ToList();
                    var updatedPerms = updated.Permissions.ToList();
                    foreach (var permission in oldPerms)
                    {
                        if (!updatedPerms.Contains(permission))
                        {
                            description.AppendLine($"Removed permission {permission}");
                        }
                    }
                    foreach (var permission in updatedPerms)
                    {
                        if (!oldPerms.Contains(permission))
                        {
                            description.AppendLine($"Added permission {permission}");
                        }
                    }
                }
                if (old.IsHoisted != updated.IsHoisted)
                {
                    description.AppendLine("Changed `Hoisted` to " + updated.IsHoisted);
                }
                if (old.IsMentionable != updated.IsMentionable)
                {
                    description.AppendLine("Changed `Mentionable` to " + updated.IsMentionable);
                }
                if (description.Length > 0)
                {
                    embed.Title = $"{updated} updated";
                    embed.Description = $"{updated.Mention}\n{description.ToString()}";
                    return channel.SendEmbedAsync(embed);
                }
            }
            return Task.CompletedTask;
        }

        private static Task Client_RoleDeleted(SocketRole arg)
        {
            if (TryGetLogChannel(arg.Guild, DiscordEventType.RoleDeleted, out SocketTextChannel channel, out EmbedBuilder embed))
            {
                embed.Title = $"{arg} deleted";
                embed.Description =
                    $"Color: {arg.Color.RawValue.ToString("x")}" +
                    $"Hierarchy: {arg.Position}\n" +
                    $"Permissions: {arg.Permissions}\n" +
                    $"Hoisted: {arg.IsHoisted}" +
                    $"Mentionable: {arg.IsMentionable}";
                return channel.SendEmbedAsync(embed);
            }
            return Task.CompletedTask;
        }

        private static Task Client_RoleCreated(SocketRole arg)
        {
            if (TryGetLogChannel(arg.Guild, DiscordEventType.RoleCreated, out SocketTextChannel channel, out EmbedBuilder embed))
            {
                embed.Title = $"{arg} created";
                embed.Description = $"{arg.Mention}";
                return channel.SendEmbedAsync(embed);
            }
            return Task.CompletedTask;
        }

        #endregion
        #region messages

        private static async Task Client_MessageUpdated(Discord.Cacheable<Discord.IMessage, ulong> old, SocketMessage updated, ISocketMessageChannel arg3)
        {
            IMessage baseMessage = await old.GetOrDownloadAsync();
            IUserMessage oldMessage = baseMessage as IUserMessage;
            IUserMessage newMessage = updated as IUserMessage;
            SocketTextChannel guildChannel = arg3 as SocketTextChannel;
            if ((oldMessage != null) && newMessage != null && guildChannel != null && !oldMessage.Author.IsBot && !oldMessage.Author.IsWebhook)
            {
                if (TryGetLogChannel(guildChannel.Guild, DiscordEventType.MessageUpdated, out SocketTextChannel logChannel, out EmbedBuilder embed))
                {
                    StringBuilder description = new StringBuilder($"Location: {guildChannel.Mention}\n" +
                        $"Author: {oldMessage.Author} ({oldMessage.Author.Mention})\n" +
                        $"Message Link: {oldMessage.GetMessageURL(guildChannel.Guild.Id)}\n");
                    if (newMessage.IsPinned != oldMessage.IsPinned)
                    {
                        description.Append("Pinned: ");
                        description.AppendLine(newMessage.IsPinned.ToString(CultureInfo.InvariantCulture));
                    }
                    bool resend = false;
                    if (newMessage.EditedTimestamp != oldMessage.EditedTimestamp)
                    {
                        embed.Title = "Message edited. For a copy see below";
                        resend = true;
                    }
                    else
                    {
                        embed.Title = "Message updated";
                    }

                    embed.Description = description.ToString();
                    await logChannel.SendEmbedAsync(embed);
                    if (resend)
                    {
                        await logChannel.SendMessageAsync(oldMessage.Content.Replace("@everyone", "@-everyone").Replace("<@&", "<Role-").Replace("<@", ",<@User-").MaxLength(EmbedHelper.MESSAGECONTENT_MAX));
                    }
                }
            }
        }

        private static async Task Client_MessagesBulkDeleted(IReadOnlyCollection<Discord.Cacheable<Discord.IMessage, ulong>> arg1, ISocketMessageChannel arg2)
        {
            foreach (var val in arg1)
            {
                await Client_MessageDeleted(val, arg2);
            }
        }

        private static async Task Client_MessageDeleted(Discord.Cacheable<Discord.IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            IMessage baseMessage = await arg1.GetOrDownloadAsync();
            IUserMessage userMessage = baseMessage as IUserMessage;
            SocketTextChannel guildChannel = arg2 as SocketTextChannel;
            if ((userMessage != null) && guildChannel != null)
            {
                if (TryGetLogChannel(guildChannel.Guild, DiscordEventType.MessageDeleted, out SocketTextChannel logChannel, out EmbedBuilder embed))
                {
                    embed.Title = "Message deleted. For a copy see below";
                    embed.Description = $"Location: {guildChannel.Mention}\n" +
                        $"Author: {userMessage.Author} ({userMessage.Author.Mention})\n" +
                        $"Original Timestamp: {userMessage.Timestamp}\n";
                    await logChannel.SendEmbedAsync(embed);
                    Embed messageEmbed = userMessage.Embeds.FirstOrDefault() as Embed;
                    await logChannel.SendMessageAsync(userMessage.Content.Replace("@everyone", "@-everyone").Replace("<@&", "<Role-").Replace("<@", ",<@User-").MaxLength(EmbedHelper.MESSAGECONTENT_MAX), embed: messageEmbed);
                }
            }
            else if (guildChannel != null)
            {
                if (TryGetLogChannel(guildChannel.Guild, DiscordEventType.MessageDeleted, out SocketTextChannel logChannel, out EmbedBuilder embed))
                {
                    embed.Title = "Message deleted. Too old to be cached";
                    embed.Description = $"Location: {guildChannel.Mention}\n" +
                        $"Author: unknown\n" +
                        $"Original Timestamp: unknown\n";
                    await logChannel.SendEmbedAsync(embed);
                }
            }
        }

        #endregion
        #region guild

        private static Task Client_GuildUpdated(SocketGuild old, SocketGuild updated)
        {
            if (TryGetLogChannel(old, DiscordEventType.GuildUpdated, out SocketTextChannel logChannel, out EmbedBuilder embed))
            {
                StringBuilder changes = new StringBuilder();
                if (old.IconUrl != updated.IconUrl)
                {
                    changes.AppendLine($"Changed Guild Icon from {old.IconUrl} to {updated.IconUrl}");
                }
                if (old.MfaLevel != updated.MfaLevel)
                {
                    changes.AppendLine($"Changed MfaLevel from {old.MfaLevel} to {updated.MfaLevel}");
                }
                if (old.Name != updated.Name)
                {
                    changes.AppendLine($"Changed Name from {old.Name} to {updated.Name}");
                }
                if (old.OwnerId != updated.OwnerId)
                {
                    changes.AppendLine($"Transferred ownership from {old.Owner} to {updated.Owner}");
                }
                if (old.SplashUrl != updated.SplashUrl)
                {
                    changes.AppendLine($"Changed SplashUrl from {old.SplashUrl} to {updated.SplashUrl}");
                }
                if (old.SystemChannel != updated.SystemChannel)
                {
                    if (old.SystemChannel == null)
                    {
                        changes.AppendLine($"Set Systemchannel to \"{updated.SystemChannel}\"");
                    }
                    else if (updated.SystemChannel == null)
                    {
                        changes.AppendLine($"Removed Systemchannel \"{old.SystemChannel}\"");
                    }
                    else
                    {
                        changes.AppendLine($"Changed Systemchannel from \"{old.SystemChannel}\" to \"{updated.SystemChannel}\"");
                    }
                }
                if (old.VerificationLevel != updated.VerificationLevel)
                {
                    changes.AppendLine($"Changed VerificationLevel from {old.VerificationLevel} to {updated.VerificationLevel}");
                }
                if (old.VoiceRegionId != updated.VoiceRegionId)
                {
                    changes.AppendLine($"Changed VoiceRegion from {old.VoiceRegionId} to {updated.VoiceRegionId}");
                }
                if (changes.Length > 0)
                {
                    embed.Description = changes.ToString();
                    return logChannel.SendEmbedAsync(embed);
                }
            }
            return Task.CompletedTask;
        }

        #endregion
        #region channels

        private static Task Client_ChannelUpdated(SocketChannel arg1, SocketChannel arg2)
        {
            SocketGuildChannel old = arg1 as SocketGuildChannel;
            SocketGuildChannel updated = arg2 as SocketGuildChannel;
            if (old != null && updated != null)
            {
                if (TryGetLogChannel(old.Guild, DiscordEventType.ChannelUpdated, out SocketTextChannel logChannel, out EmbedBuilder embed))
                {
                    StringBuilder description = new StringBuilder();
                    ChannelUpdated(old, updated, ref description);
                    if (old is SocketTextChannel textOld && updated is SocketTextChannel textUpdated)
                    {
                        ChannelUpdated(textOld, textUpdated, ref description);
                    }
                    else if (old is SocketVoiceChannel voiceOld && updated is SocketVoiceChannel voiceUpdated)
                    {
                        ChannelUpdated(voiceOld, voiceUpdated, ref description);
                    }
                    if (description.Length > 0)
                    {
                        embed.Title = $"#{updated} updated";
                        embed.Description = description.ToString();
                        return logChannel.SendEmbedAsync(embed);
                    }
                }
            }
            return Task.CompletedTask;
        }

        private static void ChannelUpdated(SocketGuildChannel old, SocketGuildChannel updated, ref StringBuilder changes)
        {
            if (old.Name != updated.Name)
            {
                changes.AppendLine($"Name changed from \"{old.Name}\" to \"{updated.Name}\"");
            }
            foreach (var oldPerm in old.PermissionOverwrites)
            {
                if (updated.PermissionOverwrites.TryFind(overwrite => { return overwrite.TargetId == oldPerm.TargetId; }, out Overwrite updatedPerm))
                {
                    if (CompareOverwritePermissions(oldPerm.Permissions, updatedPerm.Permissions, out StringBuilder permChanges, "    "))
                    {
                        changes.AppendLine($"Changed PermissionOverwrite for {GetPermOverwriteTarget(oldPerm)}");
                        changes.Append(permChanges);
                    }
                    //PrintOverwritePermission(oldPerm.Permissions, ref changes, "   ");
                    //changes.AppendLine($"New Values");
                    //PrintOverwritePermission(updatedPerm.Permissions, ref changes, "   ");
                }
                else
                {
                    changes.AppendLine($"Removed PermissionOverwrite for {GetPermOverwriteTarget(oldPerm)}");
                    PrintOverwritePermission(oldPerm.Permissions, ref changes, "   ");
                }
            }
            foreach (var updatedPerm in updated.PermissionOverwrites)
            {
                if (!old.PermissionOverwrites.Any(overwrite => { return overwrite.TargetId == updatedPerm.TargetId; }))
                {
                    changes.AppendLine($"Added PermissionOverwrite for {GetPermOverwriteTarget(updatedPerm)}");
                    PrintOverwritePermission(updatedPerm.Permissions, ref changes, "    ");
                }
            }
        }

        private static string GetPermOverwriteTarget(Overwrite overwrite)
        {
            if (overwrite.TargetType == PermissionTarget.Role)
            {
                return Markdown.Mention_Role(overwrite.TargetId);
            }
            else
            {
                return Markdown.Mention_User(overwrite.TargetId);
            }
        }

        private static void ChannelUpdated(SocketTextChannel old, SocketTextChannel updated, ref StringBuilder changes)
        {
            if (old.CategoryId != updated.CategoryId)
            {
                if (old.CategoryId == null)
                {
                    changes.AppendLine($"Moved to category \"{updated.Category}\"");
                }
                else if (updated.CategoryId == null)
                {
                    changes.AppendLine($"Removed from category \"{old.Category}\"");
                }
                else
                {
                    changes.AppendLine($"Moved from category \"{old.Category}\" to \"{updated.Category}\"");
                }
            }
            if (old.IsNsfw != updated.IsNsfw)
            {
                changes.AppendLine("Changed IsNsfw to " + updated.IsNsfw);
            }
            if (old.SlowModeInterval != updated.SlowModeInterval)
            {
                changes.AppendLine($"Changed SlowModeInterval from \"{old.SlowModeInterval}\" to \"{updated.SlowModeInterval}\"");
            }
            if (old.Topic != updated.Topic)
            {
                if (string.IsNullOrEmpty(old.Topic))
                {
                    changes.AppendLine($"Set a new topic: `{updated.Topic}`");
                }
                else
                {
                    changes.AppendLine($"Updated topic. Old topic: `{old.Topic}`");
                }
            }
        }

        private static void ChannelUpdated(SocketVoiceChannel old, SocketVoiceChannel updated, ref StringBuilder changes)
        {
            if (old.UserLimit != updated.UserLimit)
            {
                changes.AppendLine($"Changed UserLimit from {old.UserLimit} to {updated.UserLimit}");
            }
            if (old.Bitrate != updated.Bitrate)
            {
                changes.AppendLine($"Changed BitRate from {old.Bitrate} to {updated.Bitrate}");
            }
        }

        private static void PrintOverwritePermission(OverwritePermissions op, ref StringBuilder changes, string indent)
        {
            changes.Append("```");
            changes.Append(indent);
            changes.Append("Allowed: ");
            changes.AppendJoin(" | ", op.ToAllowList());
            changes.AppendLine();
            changes.Append(indent);
            changes.Append("Denied: ");
            changes.AppendJoin(" | ", op.ToDenyList());
            changes.Append("```");
            changes.AppendLine();
        }

        private static bool CompareOverwritePermissions(OverwritePermissions old, OverwritePermissions updated, out StringBuilder changes, string indent)
        {
            bool changesDetected = false;
            changes = new StringBuilder("```");
            var oldAllowList = old.ToAllowList();
            var oldDenyList = old.ToDenyList();
            var updatedAllowList = updated.ToAllowList();
            var updatedDenyList = updated.ToDenyList();
            foreach (ChannelPermission oldAllowPerm in oldAllowList)
            {
                if (!updatedAllowList.Contains(oldAllowPerm))
                {
                    changesDetected = true;
                    if (updatedDenyList.Contains(oldAllowPerm))
                    {
                        changes.Append(indent);
                        changes.AppendLine($"Denied {oldAllowPerm}");
                    }
                    else
                    {
                        changes.Append(indent);
                        changes.AppendLine($"Inherited {oldAllowPerm}");
                    }
                }
            }
            foreach (ChannelPermission oldDenyPerm in oldDenyList)
            {
                if (!updatedDenyList.Contains(oldDenyPerm))
                {
                    changesDetected = true;
                    if (updatedAllowList.Contains(oldDenyPerm))
                    {
                        changes.Append(indent);
                        changes.AppendLine($"Allowed {oldDenyPerm}");
                    }
                    else
                    {
                        changes.Append(indent);
                        changes.AppendLine($"Inherited {oldDenyPerm}");
                    }
                }
            }
            foreach (ChannelPermission updatedAllowPerm in updatedAllowList)
            {
                if (!oldDenyList.Contains(updatedAllowPerm) && !oldAllowList.Contains(updatedAllowPerm))
                {
                    changesDetected = true;
                    changes.Append(indent);
                    changes.AppendLine($"Allowed {updatedAllowPerm}");
                }
            }
            foreach (ChannelPermission updatedDenyPerm in updatedDenyList)
            {
                if (!oldDenyList.Contains(updatedDenyPerm) && !oldAllowList.Contains(updatedDenyPerm))
                {
                    changesDetected = true;
                    changes.Append(indent);
                    changes.AppendLine($"Denied {updatedDenyPerm}");
                }
            }
            changes.Append("```");
            return changesDetected;
        }

        private static Task Client_ChannelDestroyed(SocketChannel arg)
        {
            SocketGuildChannel guildChannel = arg as SocketGuildChannel;
            if ((guildChannel != null) && TryGetLogChannel(guildChannel.Guild, DiscordEventType.ChannelDestroyed, out SocketTextChannel channel, out EmbedBuilder embed))
            {
                embed.Title = $"#{guildChannel} deleted";
                embed.Description = GetChannelDescription(guildChannel);
                return channel.SendEmbedAsync(embed);
            }
            return Task.CompletedTask;
        }

        private static Task Client_ChannelCreated(SocketChannel arg)
        {
            SocketGuildChannel guildChannel = arg as SocketGuildChannel;
            if ((guildChannel != null) && TryGetLogChannel(guildChannel.Guild, DiscordEventType.ChannelCreated, out SocketTextChannel channel, out EmbedBuilder embed))
            {
                embed.Title = $"#{guildChannel} created";
                embed.Description = GetChannelDescription(guildChannel);
                return channel.SendEmbedAsync(embed);
            }
            return Task.CompletedTask;
        }

        private static string GetChannelDescription(SocketGuildChannel channel)
        {
            StringBuilder description = new StringBuilder();

            SocketTextChannel text = channel as SocketTextChannel;
            if (text != null)
            {
                description.AppendLine(text.Mention);
                description.AppendLine($"Category: {text.Category}");
            }
            SocketVoiceChannel voice = channel as SocketVoiceChannel;
            if (voice != null)
            {
                description.AppendLine($"Category: {voice.Category}");
            }
            description.AppendLine($"Hierarchy: {channel.Position}");
            description.AppendLine($"Permissions: {channel.PermissionOverwrites.Count}");
            foreach (var perm in channel.PermissionOverwrites)
            {
                description.AppendLine(perm.ToString());
            }

            return description.ToString();
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



        private static bool TryGetLogChannel(SocketGuild guild, DiscordEventType type, out SocketTextChannel channel, out EmbedBuilder baseEmbed)
        {
            if (GuildEventLoggerSettings.TryGetValue(guild.Id, out EventLoggerSettings settings))
            {
                if (settings.EventLogChannels.TryGetValue(type, out ulong channelId))
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

        private static EmbedBuilder GetBaseEmbed(DiscordEventType type)
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
