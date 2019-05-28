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
        public GuildChannelInformation ChannelInfo { get; private set; }

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
                ChannelInfo = GuildChannelHelper.GetChannelInfoOrDefault(GuildChannel);
            }
        }

        public override bool IsDefined { get { return base.IsDefined && GuildUser != null && GuildChannel != null && Guild != null; } }

        public static bool TryConvert(CommandContext context, out GuildCommandContext guildContext)
        {
            guildContext = context as GuildCommandContext;
            return guildContext != null;
        }
    }

    public class IndexArray<T> : IEnumerable<T>, ICloneable
    {
        private T[] array;

        private int baseIndex = 0;
        public int Index
        {
            get
            {
                return baseIndex;
            }
            set
            {
                if (value > array.Length || value < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
                baseIndex = value;
            }
        }
        public int Count { get { return array.Length - baseIndex; } }
        public int TotalCount { get { return array.Length; } }

        #region Constructors

        public IndexArray(int count)
        {
            array = new T[count];
        }

        public IndexArray(T[] existingItems)
        {
            array = new T[existingItems.Length];
            existingItems.CopyTo(array, 0);
        }

        public IndexArray(ICollection<T> existingItems)
        {
            array = new T[existingItems.Count];
            existingItems.CopyTo(array, 0);
        }

        #endregion
        #region Accessors

        public T this[int index]
        {
            get
            {
                return array[baseIndex + index];
            }
            set
            {
                array[baseIndex + index] = value;
            }
        }

        public T First { get { return array[baseIndex]; } }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = baseIndex; i < array.Length; i++)
            {
                yield return array[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = baseIndex; i < array.Length; i++)
            {
                yield return array[i];
            }
        }

        #endregion
        #region Conversions

        public static implicit operator IndexArray<T>(T[] from)
        {
            return new IndexArray<T>(from);
        }

        public static explicit operator T[] (IndexArray<T> from)
        {
            T[] result = new T[from.array.Length - from.baseIndex];
            for (int i = 0; i < from.Count; i++)
            {
                result[i] = from[i];
            }
            return result;
        }

        #endregion
        #region Misc

        public bool WithinBounds(int index)
        {
            return index >= 0 && index + baseIndex < array.Length;
        }

        public object Clone()
        {
            IndexArray<T> clone = new IndexArray<T>((T[])array.Clone());
            clone.baseIndex = baseIndex;
            return clone;
        }

        #endregion
    }
}