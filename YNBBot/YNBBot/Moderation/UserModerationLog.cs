using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotCoreNET;
using Discord.WebSocket;
using JSON;

namespace YNBBot.Moderation
{
    class UserModerationLog : IJSONSerializable
    {
        public readonly GuildModerationLog Parent;
        public ulong UserId { get; private set; }
        public DateTimeOffset? BannedUntil { get; private set; } = null;
        public DateTimeOffset? MutedUntil { get; private set; } = null;
        private List<ulong> rolesPreMute = null;
        private List<UserModerationEntry> moderationEntries = new List<UserModerationEntry>();
        public IReadOnlyList<UserModerationEntry> ModerationEntries => moderationEntries.AsReadOnly();

        public bool IsBanned { get { return BannedUntil != null; } }
        public TimeSpan BanTimeRemaining
        {
            get
            {
                if (BannedUntil.HasValue)
                {
                    return BannedUntil.Value - DateTimeOffset.UtcNow;
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }
        public bool IsMuted { get { return MutedUntil != null; } }
        public TimeSpan MuteTimeRemaining
        {
            get
            {
                if (MutedUntil.HasValue)
                {
                    return MutedUntil.Value - DateTimeOffset.UtcNow;
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }

        public UserModerationLog(GuildModerationLog parent, ulong userId = 0)
        {
            Parent = parent;
            UserId = userId;
        }

        public async Task AddModerationEntry(UserModerationEntry entry)
        {
            moderationEntries.Add(entry);
            await Save();
            await Parent.InvokeUserModerationLogModifiedEvents(this, entry);
        }

        public Task Save()
        {
            JSONContainer json = ToJSON();
            return ResourcesModel.WriteJSONObjectToFile($"{Parent.UserDirectory}/{UserId}.json", json);
        }

        public async Task AddBan(UserModerationEntry entry, DateTimeOffset banUntil)
        {
            moderationEntries.Add(entry);
            BannedUntil = banUntil;
            MutedUntil = null;
            if (banUntil < DateTimeOffset.MaxValue)
            {
                GuildModerationLog.AddTimeLimitedInfractionReference(this);
            }
            await Save();
            await Parent.InvokeUserModerationLogModifiedEvents(this, entry);
        }

        public async Task UnBan(SocketGuild guild, UserModerationEntry? entry = null)
        {
            BannedUntil = null;
            if (!entry.HasValue)
            {
                SocketGuildUser self = guild.GetUser(BotCore.Client.CurrentUser.Id);
                entry = new UserModerationEntry(Parent.GuildId, ModerationType.UnBanned, null, self, "Automatic Unban");
            }
            moderationEntries.Add(entry.Value);
            await Parent.InvokeUserModerationLogModifiedEvents(this, entry.Value);
            await Save();
            await guild.RemoveBanAsync(UserId);
        }

        public async Task AddMute(SocketGuildUser target, DateTimeOffset muteUntil, UserModerationEntry? entry = null)
        {
            // Generate UserModerationEntry

            if (!entry.HasValue)
            {
                SocketGuildUser self = target.Guild.GetUser(BotCore.Client.CurrentUser.Id);

                if (muteUntil == DateTimeOffset.MaxValue)
                {
                    entry = new UserModerationEntry(target.Guild.Id, ModerationType.Muted, DateTimeOffset.UtcNow, self, "Automatic Mute", "Duration: perma");
                }
                else
                {
                    entry = new UserModerationEntry(target.Guild.Id, ModerationType.Muted, DateTimeOffset.UtcNow, self, "Automatic Mute", "Duration: " + (muteUntil - DateTimeOffset.UtcNow).ToHumanTimeString());
                }
            }

            moderationEntries.Add(entry.Value);

            // Edit user roles

            List<SocketRole> roles = new List<SocketRole>(target.Roles.Count - 1);

            foreach (SocketRole role in target.Roles)
            {
                if (!role.IsEveryone)
                {
                    roles.Add(role);
                }
            }

            SocketRole muteRole = target.Guild.GetRole(SettingsModel.MuteRole);

            try
            {
                await target.RemoveRolesAsync(roles);
                if (muteRole != null)
                {
                    await target.AddRoleAsync(muteRole);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                throw;
#endif
            }

            // Edit UserModerationLog

            MutedUntil = muteUntil;
            rolesPreMute = new List<ulong>(roles.Select(role => role.Id));
            if (muteUntil < DateTimeOffset.MaxValue)
            {
                GuildModerationLog.AddTimeLimitedInfractionReference(this);
            }

            // Save

            await Save();
            await Parent.InvokeUserModerationLogModifiedEvents(this, entry.Value);
        }

        public async Task RemoveMute(SocketGuildUser guildUser, UserModerationEntry? entry = null)
        {
            MutedUntil = null;
            if (!entry.HasValue)
            {
                SocketGuildUser self = guildUser.Guild.GetUser(BotCore.Client.CurrentUser.Id);
                entry = new UserModerationEntry(Parent.GuildId, ModerationType.UnMuted, null, self, "Automatic Unmute");
            }
            moderationEntries.Add(entry.Value);
            await Parent.InvokeUserModerationLogModifiedEvents(this, entry.Value);
            await Save();

            SocketRole muteRole = guildUser.Guild.GetRole(SettingsModel.MuteRole);
            if ((muteRole != null) && guildUser.Roles.Contains(role => { return role.Id == muteRole.Id; }))
            {
                await guildUser.RemoveRoleAsync(muteRole);
            }
            if ((rolesPreMute != null) && rolesPreMute.Count > 0) {
                List<SocketRole> roles = new List<SocketRole>();
                foreach (ulong roleId in rolesPreMute)
                {
                    SocketRole role = guildUser.Guild.GetRole(roleId);
                    if (role != null && !guildUser.Roles.Any(existingRole => { return existingRole.Id == roleId; }))
                    {
                        roles.Add(role);
                    }
                }
                if (roles.Count > 0)
                {
                    await guildUser.AddRolesAsync(roles);
                }
                rolesPreMute.Clear();
            }
        }

        private const string JSON_USERID = "UserId";
        private const string JSON_BANNEDUNTIL = "BannedUntil";
        private const string JSON_MUTEDUNTIL = "MutedUntil";
        private const string JSON_ROLEIDS = "RolesPreMute";
        private const string JSON_MODENTRIES = "ModEntries";

        public bool FromJSON(JSONContainer json)
        {
            if (json.TryGetField(JSON_USERID, out ulong id))
            {
                UserId = id;
            }
            else
            {
                return false;
            }

            string timestamp_str;
            if (json.TryGetField(JSON_BANNEDUNTIL, out timestamp_str))
            {
                if (DateTimeOffset.TryParseExact(timestamp_str, "u", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset bannedUntil))
                {
                    BannedUntil = bannedUntil;
                }
                else
                {
                    BannedUntil = DateTimeOffset.MaxValue;
                }
            }
            else
            {
                BannedUntil = null;
            }
            if (json.TryGetField(JSON_MUTEDUNTIL, out timestamp_str))
            {
                if (DateTimeOffset.TryParseExact(timestamp_str, "u", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset mutedUntil))
                {
                    MutedUntil = mutedUntil;
                }
                else
                {
                    MutedUntil = DateTimeOffset.MaxValue;
                }
                if (json.TryGetField(JSON_ROLEIDS, out JSONContainer roleArray))
                {
                    rolesPreMute = new List<ulong>();
                    if (roleArray.IsArray && roleArray.Array != null)
                    {
                        foreach (JSONField arrayField in roleArray.Array)
                        {
                            if (arrayField.IsNumber && !arrayField.IsFloat)
                            {
                                rolesPreMute.Add(arrayField.Unsigned_Int64);
                            }
                        }
                    }
                }
            }
            else
            {
                MutedUntil = null;
            }

            if (json.TryGetArrayField(JSON_MODENTRIES, out JSONContainer jsonModEntries))
            {
                if (jsonModEntries.Array != null)
                {
                    foreach (JSONField jsonModEntry in jsonModEntries.Array)
                    {
                        if (jsonModEntry.IsObject)
                        {
                            UserModerationEntry moderationEntry = new UserModerationEntry(Parent.GuildId);
                            if (moderationEntry.FromJSON(jsonModEntry.Container))
                            {
                                moderationEntries.Add(moderationEntry);
                            }
                        }
                    }
                }

            }

            return BannedUntil != null || MutedUntil != null || moderationEntries.Count > 0;
        }

        public JSONContainer ToJSON()
        {
            JSONContainer result = JSONContainer.NewObject();
            result.TryAddField(JSON_USERID, UserId);
            if (BannedUntil.HasValue)
            {
                result.TryAddField(JSON_BANNEDUNTIL, BannedUntil.Value.ToString("u"));
            }
            if (MutedUntil.HasValue)
            {
                result.TryAddField(JSON_MUTEDUNTIL, MutedUntil.Value.ToString("u"));
                if ((rolesPreMute != null) && rolesPreMute.Count > 0)
                {
                    JSONContainer rolespremute = JSONContainer.NewArray();
                    foreach (ulong roleId in rolesPreMute)
                    {
                        rolespremute.Add(roleId);
                    }
                    result.TryAddField(JSON_ROLEIDS, rolespremute);
                }
            }
            if (moderationEntries.Count > 0)
            {
                JSONContainer jsonModEntries = JSONContainer.NewArray();
                foreach (UserModerationEntry entry in moderationEntries)
                {
                    jsonModEntries.Add(entry.ToJSON());
                }
                result.TryAddField(JSON_MODENTRIES, jsonModEntries);
            }
            return result;
        }
    }
}
