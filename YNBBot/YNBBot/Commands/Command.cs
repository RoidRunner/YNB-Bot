//using Discord;
//using Discord.Commands;
//using Discord.WebSocket;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;

//namespace YNBBot
//{
//    internal struct CommandOld
//    {
//        public const string NO_ARGUMENTS = "None";
//        public readonly bool async;

//        public CommandOld(CommandKeys key, HandleCommand handleCommand, AccessLevel accessLevel, string summary, string syntax, string argumentHelp = NO_ARGUMENTS, SpecialChannelType channelType = SpecialChannelType.Normal, bool useTyping = false)
//        {
//            async = true;
//            Key = key;
//            AccessLevel = accessLevel;
//            HandleCommand = handleCommand;
//            HandleSynchronousCommand = null;
//            this.summary = summary;
//            Syntax = syntax;
//            ArgumentHelp = argumentHelp;
//            RequiredChannelType = channelType;
//            IsShitposting = RequiredChannelType == SpecialChannelType.ShitpostingAllowed;
//            UseTyping = useTyping;
//        }

//        public CommandOld(CommandKeys key, HandleSynchronousCommand handleCommand, AccessLevel accessLevel, string summary, string syntax, string argumentHelp = NO_ARGUMENTS, SpecialChannelType channelType = SpecialChannelType.Normal, bool useTyping = false, bool isSynchronous = true)
//        {
//            async = false;
//            Key = key;
//            AccessLevel = accessLevel;
//            HandleCommand = null;
//            HandleSynchronousCommand = handleCommand;
//            this.summary = summary;
//            Syntax = syntax;
//            ArgumentHelp = argumentHelp;
//            RequiredChannelType = channelType;
//            IsShitposting = RequiredChannelType == SpecialChannelType.ShitpostingAllowed;
//            UseTyping = useTyping;
//        }

//        internal CommandKeys Key { get; private set; }
//        internal AccessLevel AccessLevel { get; private set; }
//        internal HandleCommand HandleCommand { get; private set; }
//        internal HandleSynchronousCommand HandleSynchronousCommand { get; private set; }
//        private readonly string summary;
//        internal string Summary {
//            get {
//                if (IsShitposting)
//                {
//                    return "(Shitposting) " + summary;
//                }
//                else
//                {
//                    return summary;
//                }
//            }
//        }
//        internal string Syntax { get; private set; }
//        internal string ArgumentHelp { get; private set; }
//        internal bool IsShitposting { get; private set; }
//        internal SpecialChannelType RequiredChannelType { get; private set; }
//        internal bool UseTyping { get; private set; }


//        public override string ToString()
//        {
//            return Syntax;
//        }

//        internal bool UserHasPermission(AccessLevel userlevel)
//        {
//            return userlevel >= AccessLevel;
//        }
//    }

//    internal delegate Task HandleCommand(CommandContextOld context);
//    internal delegate void HandleSynchronousCommand(CommandContextOld context);

//    /// <summary>
//    /// Carries all info necessary to parse commands
//    /// </summary>
//    internal struct CommandKeys
//    {
//        /// <summary>
//        /// The fixed keywords identifying the command
//        /// </summary>
//        internal string[] Keys { get; private set; }
//        /// <summary>
//        /// Count of fixed keywords
//        /// </summary>
//        internal int FixedArgCnt { get; private set; }
//        /// <summary>
//        /// Minimal count of arguments the command requires to function
//        /// </summary>
//        internal int MinArgCnt { get; private set; }
//        /// <summary>
//        /// Maximum count of arguments the command may take
//        /// </summary>
//        internal int MaxArgCnt { get; private set; }

//        internal CommandKeys(string key, int minArgCnt, int maxArgCnt)
//        {
//            Keys = key.Split(' ');
//            FixedArgCnt = Keys.Length;
//            MinArgCnt = minArgCnt;
//            MaxArgCnt = maxArgCnt;
//        }

//        internal CommandKeys(string key)
//        {
//            Keys = key.Split(' ');
//            FixedArgCnt = Keys.Length;
//            MinArgCnt = 0;
//            MaxArgCnt = FixedArgCnt;
//        }

//        internal bool Matches(string[] check)
//        {
//            int checkCnt = check.Length;
//            // Bail out if arg cnt doesn't match
//            if (checkCnt > MaxArgCnt || checkCnt < FixedArgCnt)
//            {
//                return false;
//            }
//            else
//            {
//                bool allKeysMatch = true;
//                for (int i = 0; i < Keys.Length; i++)
//                {
//                    if (!Keys[i].Equals(check[i]))
//                    {
//                        allKeysMatch = false;
//                        break;
//                    }
//                }
//                return allKeysMatch;
//            }
//        }

//        internal bool HasMinArgCnt(int argCnt)
//        {
//            return argCnt >= MinArgCnt;
//        }

//        internal string KeyList
//        {
//            get
//            {
//                StringBuilder strbuild = new StringBuilder();
//                if (Keys.Length > 1)
//                {
//                    for (int i = 0; i < Keys.Length - 1; i++)
//                    {
//                        strbuild.Append(Keys[i]);
//                        strbuild.Append(" ");
//                    }
//                }
//                strbuild.Append(Keys[Keys.Length - 1]);
//                return strbuild.ToString();
//            }
//        }
//    }

//    /// <summary>
//    /// Carries all info on the context a commmand is executed in
//    /// </summary>
//    internal class CommandContextOld : SocketCommandContext
//    {
//        internal string[] Args { get; private set; }
//        internal int ArgCnt { get; private set; }
//        internal AccessLevel UserAccessLevel { get; private set; }
//        internal bool ChannelAllowsShitposting { get; private set; }
//        internal bool ChannelIsMissionChannel { get; private set; }
//        internal SpecialChannelType ChannelType { get; private set; }

//        internal CommandContextOld(DiscordSocketClient client, SocketUserMessage msg, string[] args) : base(client, msg)
//        {
//            Args = args;
//            ArgCnt = args.Length;
//            SetUserAccesslevelAndChannelType();
//        }

//        private void SetUserAccesslevelAndChannelType()
//        {
//            UserAccessLevel = SettingsModel.GetUserAccessLevel(User as SocketGuildUser);
//            if (SettingsModel.SpecialChannels.TryGetValue(Channel.Id, out SpecialChannelType channelType))
//            {
//                ChannelType = channelType;
//                switch (channelType)
//                {
//                    case SpecialChannelType.Normal:
//                        break;
//                    case SpecialChannelType.ShitpostingAllowed:
//                        ChannelAllowsShitposting = true;
//                        break;
//                    case SpecialChannelType.DisableCommands:
//                        break;
//                }
//            }
//            else
//            {
//                ChannelType = SpecialChannelType.Normal;
//            }
//        }

//        internal CommandContextOld(DiscordSocketClient client, SocketUserMessage msg) : base(client, msg)
//        {
//            Args = msg.Content.Split(" ");
//            Args[0] = Args[0].Substring(1);
//            ArgCnt = Args.Length;
//            SetUserAccesslevelAndChannelType();
//        }
//    }
//}
