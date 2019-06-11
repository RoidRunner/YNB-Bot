using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.Interactive
{
    static class InteractiveMessageService
    {
        private static Dictionary<ulong, InteractiveMessage> InteractiveMessages = new Dictionary<ulong, InteractiveMessage>();

        private static readonly object InteractiveMessagesLock = new object();

        public static async Task ReactionAddedHandler(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            IUserMessage reactionMessage = reaction.Message.Value;
            if (reactionMessage == null)
            {
                IMessage msg = await channel.GetMessageAsync(reaction.MessageId);
                reactionMessage = await channel.GetMessageAsync(reaction.MessageId) as IUserMessage;
            }
            SocketTextChannel textChannel = channel as SocketTextChannel;

            if ((reactionMessage != null) && (textChannel != null) && reactionMessage.Author.Id == Var.client.CurrentUser.Id && reaction.User.Value.Id != Var.client.CurrentUser.Id)
            {
                if (InteractiveMessages.TryGetValue(reactionMessage.Id, out InteractiveMessage interactiveMessage))
                {
                    MessageInteractionContext context = new MessageInteractionContext(reaction, reactionMessage, textChannel);
                    if (context.IsDefined)
                    {
                        await interactiveMessage.HandleInteraction(context);
                    }
                }
            }
        }

        public static void AddInteractiveMessage(InteractiveMessage interactive)
        {
                InteractiveMessages.Add(interactive.MessageId, interactive);
        }

        public static bool HasInteractiveMessage(InteractiveMessage interactive)
        {
                return InteractiveMessages.ContainsKey(interactive.MessageId);
        }

        public static bool HasInteractiveMessage(ulong messageId)
        {
                return InteractiveMessages.ContainsKey(messageId);
        }

        public static bool RemoveInteractiveMessage(ulong messageId)
        {
                return InteractiveMessages.Remove(messageId);
        }
    }
}
