using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.API.Gateway;
using Discord.WebSocket;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json.Linq;

namespace ClamourPushServer
{
    class Program
    {
        // Convert our sync-main to an async main method
        static void Main(string[] args) => new Program().Run().GetAwaiter().GetResult();

        // Create a DiscordClient with WebSocket support
        private DiscordSocketClient client;

        private List<UserGuildSettings> userGuildSettings = new List<UserGuildSettings>();

        private async Task Run()
        {
            client = new DiscordSocketClient();

            // Place the token of your account here
            string token = "<insert token here>";

            client.ApiClient.ReceivedGatewayEvent += (code, i, arg3, arg4) =>
            {
                if (code != GatewayOpCode.Dispatch)
                {
                    return Task.CompletedTask;
                }

                if (arg3 == "READY")
                {
                    JObject readyPacket = (JObject)arg4;
                    userGuildSettings = readyPacket["user_guild_settings"].ToObject<List<UserGuildSettings>>();
                }

                return Task.CompletedTask;
            };

            // Hook into the MessageReceived event on DiscordSocketClient
            client.MessageReceived += (message) =>
            {
                if (message.Author.Id == this.client.CurrentUser.Id) return Task.CompletedTask;

                bool shouldSendMessage = false;
                if (message.Channel is IGuildChannel)
                {
                    var channel = message.Channel as IGuildChannel;
                    var guild = userGuildSettings.Where(x => x.IsMobilePushEnabled && !x.IsMuted && x.GuildId != null).FirstOrDefault(y => y.GuildId == channel.GuildId);
                    if (guild != null)
                    {
                        var channelOverride = guild.ChannelOverrides?.FirstOrDefault(x => x.ChannelId == channel.Id);
                        if (channelOverride != null)
                        {
                            switch (channelOverride.NotificationLevel)
                            {
                                case NotificationSettingLevel.AllMessages:
                                    if (!channelOverride.IsMuted)
                                    {
                                        shouldSendMessage = true;
                                    }
                                    break;
                                case NotificationSettingLevel.Mentions:
                                    if (message.MentionedUsers.Any(x => x.Id == client.CurrentUser.Id) && !channelOverride.IsMuted)
                                    {
                                        shouldSendMessage = true;
                                    }
                                    break;
                                case NotificationSettingLevel.Parent:
                                    shouldSendMessage = ShouldNotifyGuild(guild, message);
                                    break;
                            }
                        }
                        else
                        {
                            shouldSendMessage = ShouldNotifyGuild(guild, message);
                        }
                    }
                }

                if (shouldSendMessage)
                {
                    SendNotificationAsync(message.Author.Username + " (#" + message.Channel.Name + ")", message.Author.AvatarUrl ?? "https://discordapp.com/assets/1cbd08c76f8af6dddce02c5138971129.png", message.Content, client.CurrentUser.Id);
                }

                return Task.CompletedTask;
            };

            await client.LoginAsync(TokenType.User, token);

            await client.ConnectAsync();

            // Block this task until the program is exited.
            await Task.Delay(-1);
        }

        bool ShouldNotifyGuild(UserGuildSettings guild, SocketMessage message)
        {
            switch (guild.NotificationLevel)
            {
                case NotificationSettingLevel.AllMessages:
                    if (!guild.IsMuted)
                    {
                        return true;
                    }
                    break;
                case NotificationSettingLevel.Mentions:
                    if (message.MentionedUsers.Any(x => x.Id == client.CurrentUser.Id))
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        private static async void SendNotificationAsync(string title, string avatarUrl, string text, ulong userId)
        {
            NotificationHubClient hub = NotificationHubClient.CreateClientFromConnectionString("Insert listen azure endpoint here", "Clamour");

            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = title
                                },

                                new AdaptiveText()
                                {
                                    Text = text
                                }
                            },

                    AppLogoOverride = new ToastGenericAppLogo()
                    {
                        Source = avatarUrl,
                        HintCrop = ToastGenericAppLogoCrop.Circle
                    }
                }
            };

            ToastContent toastContent = new ToastContent
            {
                Visual = visual,
                Launch = $"channelHash={"To be completed as per hashes in the actual app"}"
            };

            await hub.SendWindowsNativeNotificationAsync(toastContent.GetContent(), userId.ToString());
        }
    }
}

