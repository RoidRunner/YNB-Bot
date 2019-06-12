using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.Interactive
{
    /// <summary>
    /// Contains and handles interactive messages
    /// </summary>
    static class InteractiveMessageService
    {
        private static Dictionary<ulong, InteractiveMessage> InteractiveMessages = new Dictionary<ulong, InteractiveMessage>();

        private static readonly object InteractiveMessagesLock = new object();

        /// <summary>
        /// Handles reactions added to messages
        /// </summary>
        public static async Task ReactionAddedHandler(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            IUserMessage reactionMessage;
            if (reaction.Message.IsSpecified)
            {
                reactionMessage = reaction.Message.Value;
            }
            else
            { 
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

        /// <summary>
        /// Adds an interactive message to the Interactive Message Service
        /// </summary>
        /// <param name="interactive">InteractiveMessage to add</param>
        public static void AddInteractiveMessage(InteractiveMessage interactive)
        {
                InteractiveMessages.Add(interactive.MessageId, interactive);
        }

        /// <summary>
        /// Checks for an InteractiveMessage
        /// </summary>
        /// <param name="interactive">InteractiveMessage to check for</param>
        public static bool HasInteractiveMessage(InteractiveMessage interactive)
        {
                return InteractiveMessages.ContainsKey(interactive.MessageId);
        }

        /// <summary>
        /// Checks for existance of an InteractiveMessage for a given Message Id
        /// </summary>
        /// <param name="messageId">message Id to check against</param>
        /// <returns>True if a message was found</returns>
        public static bool HasInteractiveMessage(ulong messageId)
        {
                return InteractiveMessages.ContainsKey(messageId);
        }

        /// <summary>
        /// Removes an interactive message from the service
        /// </summary>
        public static bool RemoveInteractiveMessage(ulong messageId)
        {
                return InteractiveMessages.Remove(messageId);
        }
    }
}
