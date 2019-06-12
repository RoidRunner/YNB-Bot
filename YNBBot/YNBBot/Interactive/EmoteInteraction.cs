using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.Interactive
{
    /// <summary>
    /// Represents a single action connected to an Emote
    /// </summary>
    class EmoteInteraction
    {
        /// <summary>
        /// Id of the Message this EmoteInteraction interacts with
        /// </summary>
        public ulong MessageId { get; set; }
        /// <summary>
        /// Emote that triggers this EmoteInteraction
        /// </summary>
        public IEmote Emote { get; private set; }
        /// <summary>
        /// Action performed on trigger
        /// </summary>
        public MessageInteractionDelegate Action { get; private set; }
        /// <summary>
        /// If true, no further interaction is possible after this action has performed once
        /// </summary>
        public bool InvalidateMessage { get; private set; }

        public EmoteInteraction(IEmote emote, MessageInteractionDelegate action, bool invalidateMessage = false)
        {
            Emote = emote;
            Action = action;
            InvalidateMessage = invalidateMessage;
        }

        /// <summary>
        /// Handles the action
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleAction(MessageInteractionContext context)
        {
            try
            {
                if (InteractiveMessageService.HasInteractiveMessage(MessageId))
                {
                    if (InvalidateMessage)
                    {
                        InteractiveMessageService.RemoveInteractiveMessage(MessageId);
                    }
                    if (await Action(context))
                    {
                        InteractiveMessageService.RemoveInteractiveMessage(MessageId);
                    }
                }
            }
            catch (Exception e)
            {
                await GuildChannelHelper.SendExceptionNotification(e, $"Error handling EmoteInteraction, MessageId `{MessageId}`, Emote {Emote.Name}");
            }
        }
#pragma warning disable 1998
        internal static async Task<bool> EmptyMessageInteractionMethod(MessageInteractionContext context)
#pragma warning restore 1998
        {
            return true;
        }
    }

    internal delegate Task<bool> MessageInteractionDelegate(MessageInteractionContext context);

}
