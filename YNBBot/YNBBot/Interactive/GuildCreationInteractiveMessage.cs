using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YNBBot.MinecraftGuildSystem;

namespace YNBBot.Interactive
{
    internal class GuildCreationInteractiveMessage : InteractiveMessage
    {
        public MinecraftGuild Guild;
        public List<bool> ConfirmedMembers;
        public bool AdminConfirmed;

        public GuildCreationInteractiveMessage(IUserMessage message, MinecraftGuild guild) : base(message, expirationTime: TimingThread.Millis + EXPIRATIONDELAY)
        {
            Guild = guild;
            ConfirmedMembers = new List<bool>(new bool[guild.MemberIds.Count]);
            EmoteInteraction confirmInteraction = new EmoteInteraction(UnicodeEmoteService.Checkmark, CheckmarkEmoteUsed, false);
            EmoteInteraction denyInteraction = new EmoteInteraction(UnicodeEmoteService.Cross, CrossEmoteUsed, false);
            AddMessageInteractionParams(confirmInteraction, denyInteraction);
        }

        public const long EXPIRATIONDELAY = 172800000;

        public async Task<bool> CheckmarkEmoteUsed(MessageInteractionContext context)
        {
            bool updateMessage = false;

            if (!AdminConfirmed && context.UserAccessLevel >= AccessLevel.Admin)
            {
                AdminConfirmed = true;
                updateMessage = true;
            }

            for (int i = 0; i < Guild.MemberIds.Count; i++)
            {
                if (context.User.Id == Guild.MemberIds[i])
                {
                    ConfirmedMembers[i] = !ConfirmedMembers[i];
                    updateMessage = true;
                    break;
                }
            }

            if (updateMessage)
            {
                return await UpdateMessage(context.Message, context.Guild);
            }
            else
            {
                return false;
            }
        }

        private async Task<bool> UpdateMessage(IUserMessage message, SocketGuild guild)
        {
            bool allConfirmed = AdminConfirmed;
            foreach (bool value in ConfirmedMembers)
            {
                if (!value)
                {
                    allConfirmed = false;
                }
            }
            EmbedBuilder embed;
            if (allConfirmed)
            {

                SocketGuildUser Captain = guild.GetUser(Guild.CaptainId);

                List<SocketGuildUser> Members = new List<SocketGuildUser>();
                foreach (ulong memberId in Guild.MemberIds)
                {
                    SocketGuildUser Member = guild.GetUser(memberId);
                    if (Member != null)
                    {
                        Members.Add(Member);
                    }
                }

                if (Captain != null && Members.Count >= MinecraftGuildModel.MIN_GUILDFOUNDINGMEMBERS)
                {
                    if (await MinecraftGuildModel.CreateGuildAsync(guild, Guild.Name, Guild.Color, Captain, Members))
                    {
                        embed = new EmbedBuilder()
                        {
                            Title = "Success",
                            Color = Guild.DiscordColor,
                            Description = $"Guild `{Guild.Name}` has been founded!"
                        };
                    }
                    else
                    {
                        embed = new EmbedBuilder()
                        {
                            Title = "Failure",
                            Color = Var.ERRORCOLOR,
                            Description = $"Internal Error!"
                        };
                    }

                }
                else
                {
                    embed = new EmbedBuilder()
                    {
                        Title = "Failure",
                        Color = Var.ERRORCOLOR,
                        Description = $"Internal Error or members of the guild left the server!"
                    };
                }
            }
            else
            {
                embed = UnconfirmedEmbed(AdminConfirmed, Guild, GuildId, ConfirmedMembers);
            }
            await message.ModifyAsync(MessageProperties =>
            {
                MessageProperties.Embed = embed.Build();
            });

            return allConfirmed;
        }

        public static async Task<GuildCreationInteractiveMessage> FromNewGuildAndMemberList(MinecraftGuild guild, List<SocketGuildUser> Members)
        {
            StringBuilder mentionString = new StringBuilder();

            foreach (SocketGuildUser member in Members)
            {
                guild.MemberIds.Add(member.Id);
                mentionString.Append(member.Mention);
                mentionString.Append(" ");
            }

            RestUserMessage message = await GuildChannelHelper.SendMessage(GuildChannelHelper.InteractiveMessagesChannelId, guild.DiscordColor, content: $"{mentionString}Please confirm ({UnicodeEmoteService.Checkmark}) or deny ({UnicodeEmoteService.Cross}) founding Membership in Guild `{guild.Name}` by reacting to this message! {Macros.Mention_Role(SettingsModel.AdminRole)} Please choose to confirm ({UnicodeEmoteService.Checkmark}) or deny ({UnicodeEmoteService.Cross}) founding of this guild!", embedTitle: "Setting up Interactive Message - Stand By");
            if (message != null)
            {
                GuildCreationInteractiveMessage result = new GuildCreationInteractiveMessage(message as IUserMessage, guild);
                await message.AddReactionsAsync(new IEmote[] { UnicodeEmoteService.Checkmark, UnicodeEmoteService.Cross });
                await result.UpdateMessage(message as IUserMessage, Members[0].Guild);
                return result;
            }
            else
            {
                return null;
            }
        }

        public static EmbedBuilder UnconfirmedEmbed(bool adminConfirmed, MinecraftGuild minecraftGuild, ulong guildId, List<bool> confirmedMembers)
        {
            EmbedBuilder embed;
            StringBuilder description = new StringBuilder();
            description.AppendLine($"Admin Confirmation: {(adminConfirmed ? "Granted" : "*Pending*")}");
            for (int i = 0; i < minecraftGuild.MemberIds.Count; i++)
            {
                ulong memberId = (ulong)minecraftGuild.MemberIds[i];
                SocketGuildUser member = null;
                SocketGuild guild = Var.client.GetGuild(guildId);
                if (guild != null)
                {
                    member = guild.GetUser(memberId);
                }

                description.AppendLine($"{(member == null ? memberId.ToString() : member.Mention)} Founding Membership Confirmation: {(confirmedMembers[i] ? "Granted" : "*Pending*")}");
            }
            embed = new EmbedBuilder()
            {
                Title = $"Founding of Guild \"{minecraftGuild.Name}\" - Waiting for confirmations",
                Color = minecraftGuild.DiscordColor,
                Description = description.ToString()
            };
            return embed;
        }

        public override async Task OnMessageExpire(MessageInteractionContext context)
        {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Title = $"Founding of Guild \"{Guild.Name}\" - Failed due to Timeout!",
                    Color = Var.ERRORCOLOR
                };

                await context.Message.ModifyAsync(MessageProperties =>
                {
                    MessageProperties.Embed = embed.Build();
                });
        }

        public async Task<bool> CrossEmoteUsed(MessageInteractionContext context)
        {
            if (context.UserAccessLevel >= AccessLevel.Admin)
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Title = $"Founding of Guild \"{Guild.Name}\" - Founding rights denied!",
                    Color = Var.ERRORCOLOR
                };
                await context.Message.ModifyAsync(MessageProperties =>
                {
                    MessageProperties.Embed = embed.Build();
                });

                return true;
            }
            else if (Guild.MemberIds.Contains(context.User.Id))
            {
                EmbedBuilder embed = new EmbedBuilder()
                {
                    Title = $"Founding of Guild \"{Guild.Name}\" - Founding member {context.User.Username} denied!",
                    Color = Var.ERRORCOLOR
                };
                await context.Message.ModifyAsync(MessageProperties =>
                {
                    MessageProperties.Embed = embed.Build();
                });

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
