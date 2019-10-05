using BotCoreNET;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace YNBBot.EventLogging
{
    abstract class DiscordEvent
    {
        public SocketGuildUser Actor { get; protected set; }
        public EventType Type { get; protected set; }

        public enum EventType
        {
            ChannelCreated,
            ChannelDestroyed,
            ChannelUpdated,

            GuildMemberUpdated,
            GuildUpdated,

            MessageDeleted,
            MessageBulkDeleted,
            MessageUpdated,

            RoleCreated,
            RoleDeleted,
            RoleUpdated,

            UserJoined,
            UserLeft,
            UserBanned,
            UserUnbanned,

            UserVoiceStatusUpdated
        }

        protected DiscordEvent(SocketGuildUser actor, EventType type)
        {
            Actor = actor;
            Type = type;
        }

        public abstract EmbedBuilder ToEmbed();

        protected EmbedBuilder getBuilder()
        {
            return new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = Type.ToString()
                },
                Color = BotCore.EmbedColor,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    #region channels

    class ChannelCreatedOrDestroyedEvent : DiscordEvent
    {
        public SocketGuildChannel Channel { get; private set; }
        public SocketTextChannel TextChannel { get; private set; }
        public SocketVoiceChannel VoiceChannel { get; private set; }
        public ICategoryChannel CategoryChannel { get; private set; }
        public ChannelType ChannelType { get; private set; }

        public ChannelCreatedOrDestroyedEvent(SocketGuildChannel channel, bool created) : base(null, created ? EventType.ChannelCreated : EventType.ChannelDestroyed)
        {
            Channel = channel;
            TextChannel = channel as SocketTextChannel;
            VoiceChannel = channel as SocketVoiceChannel;
            CategoryChannel = channel as ICategoryChannel;
            if (TextChannel != null)
            {
                ChannelType = ChannelType.Text;
                CategoryChannel = TextChannel.Category;
            }
            else if (VoiceChannel != null)
            {
                ChannelType = ChannelType.Voice;
                CategoryChannel = VoiceChannel.Category;
            }
            else
            {
                ChannelType = ChannelType.Category;
            }
        }

        public override EmbedBuilder ToEmbed()
        {
            EmbedBuilder embed = getBuilder();
            embed.Title = Channel.Name;

            if ((ChannelType == ChannelType.Text || ChannelType == ChannelType.Voice) && CategoryChannel != null)
            {
                embed.Description = $"Type: `{ChannelType}`, Category `{CategoryChannel}`, Id: `{Channel.Id}`";
            }
            else
            {
                embed.Description = $"Type: `{ChannelType}`, Id: `{Channel.Id}`";
            }

            return embed;
        }
    }

    #endregion
}
