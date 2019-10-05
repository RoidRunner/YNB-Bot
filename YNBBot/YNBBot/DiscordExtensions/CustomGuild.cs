using BotCoreNET;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YNBBot.Moderation;

namespace YNBBot.DiscordExtensions
{
    class CustomGuild : IGuild
    {
        public readonly SocketGuild Guild;

        #region IGuild interface redirect
        public string Name => Guild.Name;
        public int AFKTimeout => Guild.AFKTimeout;
        public bool IsEmbeddable => Guild.IsEmbeddable;
        public DefaultMessageNotifications DefaultMessageNotifications => Guild.DefaultMessageNotifications;
        public MfaLevel MfaLevel => Guild.MfaLevel;
        public VerificationLevel VerificationLevel => Guild.VerificationLevel;
        public ExplicitContentFilterLevel ExplicitContentFilter => Guild.ExplicitContentFilter;
        public string IconId => Guild.IconId;
        public string IconUrl => Guild.IconUrl;
        public string SplashId => Guild.SplashId;
        public string SplashUrl => Guild.SplashUrl;
        public bool Available => ((IGuild)Guild).Available;
        public ulong? AFKChannelId => ((IGuild)Guild).AFKChannelId;
        public ulong DefaultChannelId => ((IGuild)Guild).DefaultChannelId;
        public ulong? EmbedChannelId => ((IGuild)Guild).EmbedChannelId;
        public ulong? SystemChannelId => ((IGuild)Guild).SystemChannelId;
        public ulong OwnerId => Guild.OwnerId;
        public ulong? ApplicationId => Guild.ApplicationId;
        public string VoiceRegionId => Guild.VoiceRegionId;
        public IAudioClient AudioClient => ((IGuild)Guild).AudioClient;
        public IRole EveryoneRole => ((IGuild)Guild).EveryoneRole;
        public IReadOnlyCollection<GuildEmote> Emotes => Guild.Emotes;
        public IReadOnlyCollection<string> Features => Guild.Features;
        public IReadOnlyCollection<IRole> Roles => ((IGuild)Guild).Roles;
        public DateTimeOffset CreatedAt => Guild.CreatedAt;
        public ulong Id => Guild.Id;
        public Task AddBanAsync(IUser user, int pruneDays = 0, string reason = null, RequestOptions options = null)
        {
            return Guild.AddBanAsync(user, pruneDays, reason, options);
        }
        public Task AddBanAsync(ulong userId, int pruneDays = 0, string reason = null, RequestOptions options = null)
        {
            return Guild.AddBanAsync(userId, pruneDays, reason, options);
        }
        public Task<IGuildUser> AddGuildUserAsync(ulong userId, string accessToken, Action<AddGuildUserProperties> func = null, RequestOptions options = null)
        {
            return ((IGuild)Guild).AddGuildUserAsync(userId, accessToken, func, options);
        }
        public Task<ICategoryChannel> CreateCategoryAsync(string name, Action<GuildChannelProperties> func = null, RequestOptions options = null)
        {
            return ((IGuild)Guild).CreateCategoryAsync(name, func, options);
        }
        public Task<GuildEmote> CreateEmoteAsync(string name, Image image, Optional<IEnumerable<IRole>> roles = default, RequestOptions options = null)
        {
            return Guild.CreateEmoteAsync(name, image, roles, options);
        }
        public Task<IGuildIntegration> CreateIntegrationAsync(ulong id, string type, RequestOptions options = null)
        {
            return ((IGuild)Guild).CreateIntegrationAsync(id, type, options);
        }
        public Task<IRole> CreateRoleAsync(string name, GuildPermissions? permissions = null, Color? color = null, bool isHoisted = false, RequestOptions options = null)
        {
            return ((IGuild)Guild).CreateRoleAsync(name, permissions, color, isHoisted, options);
        }
        public Task<ITextChannel> CreateTextChannelAsync(string name, Action<TextChannelProperties> func = null, RequestOptions options = null)
        {
            return ((IGuild)Guild).CreateTextChannelAsync(name, func, options);
        }
        public Task<IVoiceChannel> CreateVoiceChannelAsync(string name, Action<VoiceChannelProperties> func = null, RequestOptions options = null)
        {
            return ((IGuild)Guild).CreateVoiceChannelAsync(name, func, options);
        }
        public Task DeleteAsync(RequestOptions options = null)
        {
            return Guild.DeleteAsync(options);
        }
        public Task DeleteEmoteAsync(GuildEmote emote, RequestOptions options = null)
        {
            return Guild.DeleteEmoteAsync(emote, options);
        }
        public Task DownloadUsersAsync()
        {
            return Guild.DownloadUsersAsync();
        }
        public Task<IVoiceChannel> GetAFKChannelAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetAFKChannelAsync(mode, options);
        }
        public Task<IReadOnlyCollection<IAuditLogEntry>> GetAuditLogsAsync(int limit = 100, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetAuditLogsAsync(limit, mode, options);
        }
        public Task<IBan> GetBanAsync(IUser user, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetBanAsync(user, options);
        }
        public Task<IBan> GetBanAsync(ulong userId, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetBanAsync(userId, options);
        }
        public Task<IReadOnlyCollection<IBan>> GetBansAsync(RequestOptions options = null)
        {
            return ((IGuild)Guild).GetBansAsync(options);
        }
        public Task<IReadOnlyCollection<ICategoryChannel>> GetCategoriesAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetCategoriesAsync(mode, options);
        }
        public Task<IGuildChannel> GetChannelAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetChannelAsync(id, mode, options);
        }
        public Task<IReadOnlyCollection<IGuildChannel>> GetChannelsAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetChannelsAsync(mode, options);
        }
        public Task<IGuildUser> GetCurrentUserAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetCurrentUserAsync(mode, options);
        }
        public Task<ITextChannel> GetDefaultChannelAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetDefaultChannelAsync(mode, options);
        }
        public Task<IGuildChannel> GetEmbedChannelAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetEmbedChannelAsync(mode, options);
        }
        public Task<GuildEmote> GetEmoteAsync(ulong id, RequestOptions options = null)
        {
            return Guild.GetEmoteAsync(id, options);
        }
        public Task<IReadOnlyCollection<IGuildIntegration>> GetIntegrationsAsync(RequestOptions options = null)
        {
            return ((IGuild)Guild).GetIntegrationsAsync(options);
        }
        public Task<IReadOnlyCollection<IInviteMetadata>> GetInvitesAsync(RequestOptions options = null)
        {
            return ((IGuild)Guild).GetInvitesAsync(options);
        }
        public Task<IGuildUser> GetOwnerAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetOwnerAsync(mode, options);
        }
        public IRole GetRole(ulong id)
        {
            return ((IGuild)Guild).GetRole(id);
        }
        public Task<ITextChannel> GetSystemChannelAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetSystemChannelAsync(mode, options);
        }
        public Task<ITextChannel> GetTextChannelAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetTextChannelAsync(id, mode, options);
        }
        public Task<IReadOnlyCollection<ITextChannel>> GetTextChannelsAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetTextChannelsAsync(mode, options);
        }
        public Task<IGuildUser> GetUserAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetUserAsync(id, mode, options);
        }
        public Task<IReadOnlyCollection<IGuildUser>> GetUsersAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetUsersAsync(mode, options);
        }
        public Task<IInviteMetadata> GetVanityInviteAsync(RequestOptions options = null)
        {
            return ((IGuild)Guild).GetVanityInviteAsync(options);
        }
        public Task<IVoiceChannel> GetVoiceChannelAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetVoiceChannelAsync(id, mode, options);
        }
        public Task<IReadOnlyCollection<IVoiceChannel>> GetVoiceChannelsAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetVoiceChannelsAsync(mode, options);
        }
        public Task<IReadOnlyCollection<IVoiceRegion>> GetVoiceRegionsAsync(RequestOptions options = null)
        {
            return ((IGuild)Guild).GetVoiceRegionsAsync(options);
        }
        public Task<IWebhook> GetWebhookAsync(ulong id, RequestOptions options = null)
        {
            return ((IGuild)Guild).GetWebhookAsync(id, options);
        }
        public Task<IReadOnlyCollection<IWebhook>> GetWebhooksAsync(RequestOptions options = null)
        {
            return ((IGuild)Guild).GetWebhooksAsync(options);
        }
        public Task LeaveAsync(RequestOptions options = null)
        {
            return Guild.LeaveAsync(options);
        }
        public Task ModifyAsync(Action<GuildProperties> func, RequestOptions options = null)
        {
            return Guild.ModifyAsync(func, options);
        }
        public Task ModifyEmbedAsync(Action<GuildEmbedProperties> func, RequestOptions options = null)
        {
            return Guild.ModifyEmbedAsync(func, options);
        }
        public Task<GuildEmote> ModifyEmoteAsync(GuildEmote emote, Action<EmoteProperties> func, RequestOptions options = null)
        {
            return Guild.ModifyEmoteAsync(emote, func, options);
        }
        public Task<int> PruneUsersAsync(int days = 30, bool simulate = false, RequestOptions options = null)
        {
            return Guild.PruneUsersAsync(days, simulate, options);
        }
        public Task RemoveBanAsync(IUser user, RequestOptions options = null)
        {
            return Guild.RemoveBanAsync(user, options);
        }
        public Task RemoveBanAsync(ulong userId, RequestOptions options = null)
        {
            return Guild.RemoveBanAsync(userId, options);
        }
        public Task ReorderChannelsAsync(IEnumerable<ReorderChannelProperties> args, RequestOptions options = null)
        {
            return Guild.ReorderChannelsAsync(args, options);
        }
        public Task ReorderRolesAsync(IEnumerable<ReorderRoleProperties> args, RequestOptions options = null)
        {
            return Guild.ReorderRolesAsync(args, options);
        }
        #endregion

        public GuildModerationLog ModerationLog { get; private set; }

        private CustomGuild(SocketGuild guild)
        {
            ModerationLog = GuildModerationLog.GetOrCreateGuildModerationLog(guild.Id);
        }

        private Dictionary<ulong, CustomGuildUser> customGuildUsers = new Dictionary<ulong, CustomGuildUser>();

        public CustomGuildUser GetCustomGuildUser(ulong userId)
        {
            return null;
        }

        private static Dictionary<ulong, CustomGuild> loadedCustomGuilds = new Dictionary<ulong, CustomGuild>();
        public static CustomGuild GetCustomGuild(ulong guildId)
        {
            CustomGuild guild;
            if (loadedCustomGuilds.TryGetValue(guildId, out guild))
            {
                return guild;
            }
            else
            {
                SocketGuild socketGuild = BotCore.Client.GetGuild(guildId);
                if (socketGuild != null)
                {
                    guild = new CustomGuild(socketGuild);
                    loadedCustomGuilds.Add(guildId, guild);
                    return guild;
                }
                else
                {
                    return null;
                }
            }
        }
        public static CustomGuild GetCustomGuild(SocketGuild socketGuild)
        {
            CustomGuild guild;
            if (loadedCustomGuilds.TryGetValue(socketGuild.Id, out guild))
            {
                return guild;
            }
            else
            {
                guild = new CustomGuild(socketGuild);
                loadedCustomGuilds.Add(socketGuild.Id, guild);
                return guild;
            }
        }
    }
}
