using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.Interactive
{
    class EmoteInteraction
    {
        public ulong MessageId;
        public IEmote Emote;
        public MessageInteractionDelegate Action;
        public bool InvalidateMessage;

        public EmoteInteraction(IEmote emote, MessageInteractionDelegate action, bool invalidateMessage = true)
        {
            Emote = emote;
            Action = action;
            InvalidateMessage = invalidateMessage;
        }

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
