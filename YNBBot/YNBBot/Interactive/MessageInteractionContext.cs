using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace YNBBot.Interactive
{
    /// <summary>
    /// Provides context for InteractiveMessageHandlers
    /// </summary>
    class MessageInteractionContext
    {
        /// <summary>
        /// Guild of the message
        /// </summary>
        public SocketGuild Guild { get; private set; }
        /// <summary>
        /// Channel of the message
        /// </summary>
        public SocketTextChannel Channel { get; private set; }
        /// <summary>
        /// Message the reaction was added to
        /// </summary>
        public IUserMessage Message { get; private set; }
        /// <summary>
        /// User that added the reaction
        /// </summary>
        public SocketGuildUser User { get; private set; }
        /// <summary>
        /// AccessLevel of the User
        /// </summary>
        public AccessLevel UserAccessLevel { get; private set; }
        /// <summary>
        /// Reaction that was added
        /// </summary>
        public IEmote Emote { get; private set; }

        public MessageInteractionContext(SocketReaction reaction, IUserMessage message, SocketTextChannel channel)
        {
            Emote = reaction.Emote;
            Message = message;
            User = reaction.User.Value as SocketGuildUser;
            Channel = channel;
            Guild = Channel.Guild;
            if (User != null)
            {
                UserAccessLevel = Var.client.GetAccessLevel(User.Id);
            }
        }

        public bool IsDefined { get { return Guild != null && Channel != null && Message != null && User != null && Emote != null; } }
    }
}
