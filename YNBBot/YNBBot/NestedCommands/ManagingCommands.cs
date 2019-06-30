using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YNBBot.NestedCommands
{
    class SetUserNicknameCommand : Command
    {
        public override OverriddenMethod CommandHandlerMethod => OverriddenMethod.GuildAsync;
        public override OverriddenMethod ArgumentParserMethod => OverriddenMethod.GuildSynchronous;

        public SetUserNicknameCommand(string identifier) : base(identifier, AccessLevel.Admin)
        {
            CommandArgument[] arguments = new CommandArgument[]
            {
                new CommandArgument("User", "The user you want to assing a new nickname to"),
                new CommandArgument("Nickname", "The nickname you want to assign. If it contains whitespace characters, encase in quotes!", multiple:true)
            };
            InitializeHelp("Updates a users nickname", arguments);
        }

        SocketGuildUser TargetUser;
        string NewNickname;

        protected override ArgumentParseResult TryParseArgumentsGuildSynchronous(GuildCommandContext context)
        {
            if (!ArgumentParsing.TryParseGuildUser(context, context.Args.First, out TargetUser))
            {
                return new ArgumentParseResult(Arguments[0], $"Could not parse {context.Args.First} to a discord guild user!");
            }

            context.Args.Index++;

            NewNickname = ArgumentParsing.ParseMultiBlockArgument(context.Args);

            return ArgumentParseResult.SuccessfullParse;
        }

        protected override async Task HandleCommandGuildAsync(GuildCommandContext context)
        {
            if (NewNickname == "reset")
            {
                NewNickname = null;
            }
            await TargetUser.ModifyAsync(GuildUserProperties => { GuildUserProperties.Nickname = NewNickname; });
            await context.Channel.SendEmbedAsync($"Successfully renamed {TargetUser.Mention}!");
        }
    }
}
