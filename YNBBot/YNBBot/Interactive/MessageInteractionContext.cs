using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace YNBBot.Interactive
{
    class MessageInteractionContext
    {
        public SocketGuild Guild;
        public SocketTextChannel Channel;
        public IUserMessage Message;
        public SocketGuildUser User;
        public AccessLevel UserAccessLevel;
        public IEmote Emote;

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
