using BotCoreNET.Helpers;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot
{
    class AutoExpandingMessage
    {
        public string Title { get; set; }
        public EmbedAuthorBuilder Author { get; set; }
        public EmbedFooterBuilder Footer { get; set; }
        public Color Color { get; set; }
        public string ImageUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public int Length { get; private set; }
        private readonly LinkedList<string> content = new LinkedList<string>();

        public AutoExpandingMessage(string title = null, EmbedAuthorBuilder author = null, EmbedFooterBuilder footer = null)
        {
            Title = title;
            Author = author;
            Footer = footer;
        }

        public void AddLine(string line)
        {
            content.AddLast(line);
        }

        public void AddLine(object value)
        {
            AddLine(value.ToString());
        }

        public async Task Send(ISocketMessageChannel channel)
        {
            List<EmbedBuilder> embeds = new List<EmbedBuilder>(1);

            StringBuilder currentField = new StringBuilder();
            EmbedBuilder currentEmbed = getEmbedBuilder();
            foreach (string line in content)
            {
                Append(embeds, currentField, currentEmbed, line);
            }

            if (currentField.Length > 0)
            {
                Append(embeds, currentField, currentEmbed, string.Empty);
                currentEmbed.AddField("\0", currentField);
                embeds.Add(currentEmbed);
            }

            if (embeds.Count > 1)
            {
                for (int i = 0; i < embeds.Count; i++)
                {
                    embeds[i].Title += $" ({i + 1}/{embeds.Count})";
                }
            }

            foreach (EmbedBuilder embed in embeds)
            {
                await channel.SendEmbedAsync(embed);
            }
        }

        private void Append(List<EmbedBuilder> embeds, StringBuilder currentField, EmbedBuilder currentEmbed, string line)
        {
            if (currentEmbed.Length + line.Length + 10 > EmbedHelper.EMBEDTOTALLENGTH_MAX)
            {
                embeds.Add(currentEmbed);
                currentEmbed = getEmbedBuilder();
            }
            if (currentField.Length + line.Length > EmbedHelper.EMBEDFIELDVALUE_MAX)
            {
                currentEmbed.AddField("\0", currentField);
                currentField.Clear();
            }
            currentField.AppendLine(line);
        }

        private EmbedBuilder getEmbedBuilder()
        {
            return new EmbedBuilder()
            {
                Title = Title,
                Author = Author,
                Footer = Footer,
                Color = Color,
                Timestamp = Timestamp,
                ImageUrl = ImageUrl,
                ThumbnailUrl = ThumbnailUrl,
                Url = Url,
                Description = Description
            };
        }
    }
}
