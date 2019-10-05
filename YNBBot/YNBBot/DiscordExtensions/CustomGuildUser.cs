using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YNBBot.Moderation;

namespace YNBBot.DiscordExtensions
{
    class CustomGuildUser : IGuildUser
    {
        public CustomGuild CustomGuild { get; private set; }
        public UserModerationLog ModerationLog { get; private set; }

        public readonly SocketGuildUser GuildUser;

        #region IGuildUser interface redirects
        public DateTimeOffset? JoinedAt => GuildUser.JoinedAt;
        public string Nickname => GuildUser.Nickname;
        public GuildPermissions GuildPermissions => GuildUser.GuildPermissions;
        public IGuild Guild
        {
            get
            {
                if (CustomGuild == null)
                {
                    return ((IGuildUser)GuildUser).Guild;
                }
                else
                {
                    return CustomGuild;
                }
            }
        }

        public ulong GuildId => ((IGuildUser)GuildUser).GuildId;
        public IReadOnlyCollection<ulong> RoleIds => ((IGuildUser)GuildUser).RoleIds;
        public string AvatarId => GuildUser.AvatarId;
        public string Discriminator => GuildUser.Discriminator;
        public ushort DiscriminatorValue => GuildUser.DiscriminatorValue;
        public bool IsBot => GuildUser.IsBot;
        public bool IsWebhook => GuildUser.IsWebhook;
        public string Username => GuildUser.Username;
        public DateTimeOffset CreatedAt => GuildUser.CreatedAt;
        public ulong Id => GuildUser.Id;
        public string Mention => GuildUser.Mention;
        public IActivity Activity => GuildUser.Activity;
        public UserStatus Status => GuildUser.Status;
        public bool IsDeafened => GuildUser.IsDeafened;
        public bool IsMuted => GuildUser.IsMuted;
        public bool IsSelfDeafened => GuildUser.IsSelfDeafened;
        public bool IsSelfMuted => GuildUser.IsSelfMuted;
        public bool IsSuppressed => GuildUser.IsSuppressed;
        public IVoiceChannel VoiceChannel => ((IGuildUser)GuildUser).VoiceChannel;
        public string VoiceSessionId => GuildUser.VoiceSessionId;
        public Task AddRoleAsync(IRole role, RequestOptions options = null)
        {
            return GuildUser.AddRoleAsync(role, options);
        }
        public Task AddRolesAsync(IEnumerable<IRole> roles, RequestOptions options = null)
        {
            return GuildUser.AddRolesAsync(roles, options);
        }
        public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
        {
            return GuildUser.GetAvatarUrl(format, size);
        }
        public string GetDefaultAvatarUrl()
        {
            return GuildUser.GetDefaultAvatarUrl();
        }
        public Task<IDMChannel> GetOrCreateDMChannelAsync(RequestOptions options = null)
        {
            return GuildUser.GetOrCreateDMChannelAsync(options);
        }
        public ChannelPermissions GetPermissions(IGuildChannel channel)
        {
            return GuildUser.GetPermissions(channel);
        }
        public Task KickAsync(string reason = null, RequestOptions options = null)
        {
            return GuildUser.KickAsync(reason, options);
        }
        public Task ModifyAsync(Action<GuildUserProperties> func, RequestOptions options = null)
        {
            return GuildUser.ModifyAsync(func, options);
        }
        public Task RemoveRoleAsync(IRole role, RequestOptions options = null)
        {
            return GuildUser.RemoveRoleAsync(role, options);
        }
        public Task RemoveRolesAsync(IEnumerable<IRole> roles, RequestOptions options = null)
        {
            return GuildUser.RemoveRolesAsync(roles, options);
        }
        #endregion
    }
}
