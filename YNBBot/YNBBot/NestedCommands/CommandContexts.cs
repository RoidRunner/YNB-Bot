using Discord;
using System;
using Discord.WebSocket;
using System.Collections;
using System.Collections.Generic;

namespace YNBBot.NestedCommands
{
    public class CommandContext
    {
        public SocketUser User { get; private set; }
        public AccessLevel UserAccessLevel { get; private set; } = AccessLevel.Basic;

        public ISocketMessageChannel Channel { get; private set; }

        public SocketUserMessage Message { get; private set; }

        public IndexArray<string> Args { get; private set; }
        public int RawArgCnt => Args.TotalCount;

        public bool IsGuildContext { get; protected set; }

        public CommandContext(DiscordSocketClient client, SocketUserMessage message)
        {
            User = message.Author;
            if (User != null)
            {
                UserAccessLevel = Var.client.GetAccessLevel(User.Id);
            }
            Channel = message.Channel;
            Message = message;
            Args = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (RawArgCnt >= 1)
            {
                if (Args[0].Length > 0)
                {
                    Args[0] = Args[0].Substring(1);
                }
            }
            IsGuildContext = false;
        }

        public virtual bool IsDefined { get { return User != null && Channel != null && Message != null && Args != null; } }
    }

    public class GuildCommandContext : CommandContext
    {
        public GuildChannelConfiguration ChannelConfig { get; private set; }

        public SocketGuildUser GuildUser { get; private set; }

        public SocketTextChannel GuildChannel { get; private set; }

        public SocketGuild Guild { get; private set; }

        public GuildCommandContext(DiscordSocketClient client, SocketUserMessage message, SocketGuild guild) : base(client, message)
        {
            if (base.IsDefined)
            {
                GuildUser = guild.GetUser(message.Author.Id);
                GuildChannel = guild.GetTextChannel(Channel.Id);
                Guild = guild;
                IsGuildContext = true;
                ChannelConfig = GuildChannelHelper.GetChannelConfigOrDefault(GuildChannel);
            }
        }

        public override bool IsDefined { get { return base.IsDefined && GuildUser != null && GuildChannel != null && Guild != null; } }

        public static bool TryConvert(CommandContext context, out GuildCommandContext guildContext)
        {
            guildContext = context as GuildCommandContext;
            return guildContext != null;
        }
    }
}