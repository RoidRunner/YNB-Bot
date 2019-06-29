using System;
using System.Collections.Generic;
using System.Text;
using YNBBot.MinecraftGuildSystem;
using Discord;
using YNBBot.NestedCommands;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace YNBBot.Interactive
{
    class GuildDesyncInteractiveMessage : InteractiveMessage
    {
        private IndexArray<DesyncItem> Desyncs;

        public GuildDesyncInteractiveMessage(IUserMessage message, IndexArray<DesyncItem> desyncs) : base(message, null, 300000)
        {
            Desyncs = desyncs;
        }

        private static async Task MoveNext(ISocketMessageChannel channel, IndexArray<DesyncItem> desyncs)
        {
            var embed = desyncs.First.ToEmbed();
            embed.Footer = new EmbedFooterBuilder() { Text = "Desyncs left: " + desyncs.Count };
            var message = await channel.SendEmbedAsync(embed);
            GuildDesyncInteractiveMessage interactiveMessage = new GuildDesyncInteractiveMessage(message, desyncs);
            IEmote[] emotes = new IEmote[desyncs.First.Options.Count];
            for (int i = 0; i < emotes.Length; i++)
            {
                emotes[i] = UnicodeEmoteService.Numbers[i];
            }
            await message.AddReactionsAsync(emotes);
        }


        protected override async Task<bool> OnAnyEmoteAdded(MessageInteractionContext context)
        {
            if (UnicodeEmoteService.TryParseEmoteToInt(context.Emote, out int optionNumber))
            {
                if (optionNumber < Desyncs.First.Options.Count)
                {
                    await Desyncs.First.Options[optionNumber].ExecuteAsync();
                    if (Desyncs.Count > 1)
                    {
                        Desyncs.Index++;
                        await MoveNext(context.Channel, Desyncs);
                    }
                    else
                    {
                        await context.Channel.SendEmbedAsync("All desyncs dealt with!");
                    }
                    return true;
                }
            }
            return false;
        }

        public static async Task Create(ISocketMessageChannel channel, ICollection<DesyncItem> desyncs)
        {
            await MoveNext(channel, new IndexArray<DesyncItem>(desyncs));
        }
    }
}
