using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using BotCoreNET;

namespace YNBBot.MinecraftGuildSystem
{
    class DesyncItem
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public List<DesyncOption> Options = new List<DesyncOption>();

        public DesyncItem(string title, string description, params DesyncOption[] options)
        {
            Title = title;
            Description = description;
            Options.Add(new DismissDesyncOption());
            Options.AddRange(options);
        }

        public DesyncItem(string title, string description)
        {
            Title = title;
            Description = description;
            Options.Add(new DismissDesyncOption());
        }

        public void AddOptions(params DesyncOption[] options)
        {
            Options.AddRange(options);
        }

        public EmbedBuilder ToEmbed()
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Title = Title,
                Description = Description,
                Color = BotCore.EmbedColor
            };
            StringBuilder options = new StringBuilder();
            for (int i = 0; i < Options.Count; i++)
            {
                DesyncOption option = Options[i];
                options.Append($"{UnicodeEmoteService.Numbers[i]} - ");
                options.AppendLine(option.Description);
            }
            embed.AddField("Resolving Options", options);
            return embed;
        }
    }
}
