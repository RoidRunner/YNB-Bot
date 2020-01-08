using BotCoreNET.CommandHandling;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace YNBBot.Reactions
{
    internal struct ReactionContext
    {
        internal IMessage Message { get; private set; }
        internal SocketGuildUser User { get; private set; }
        internal SocketTextChannel Channel { get; private set; }
        internal SocketReaction Reaction { get; private set; }

        public ReactionContext(IUserMessage message, SocketGuildUser user, SocketTextChannel channel, SocketReaction reaction)
        {
            Message = message;
            User = user;
            Channel = channel;
            Reaction = reaction;
        }
    }

    internal struct ReactionCommand
    {
        internal string Emote;
        internal HandleReaction HandleReaction;
        internal bool IsShitposting { get; private set; }

        public ReactionCommand(string emote, HandleReaction handleReaction, bool isShitPosting = false)
        {
            Emote = emote;
            HandleReaction = handleReaction;
            IsShitposting = isShitPosting;
        }
    }
}
