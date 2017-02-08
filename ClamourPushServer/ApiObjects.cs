using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ClamourPushServer
{
    public class UserGuildSettings
    {
        [JsonProperty("suppress_everyone")]
        public bool SuppressEveryone { get; set; }

        [JsonProperty("muted")]
        public bool IsMuted { get; set; }

        [JsonProperty("mobile_push")]
        public bool IsMobilePushEnabled { get; set; }

        [JsonProperty("message_notifications")]
        public NotificationSettingLevel NotificationLevel { get; set; }

        [JsonProperty("guild_id")]
        public ulong? GuildId { get; set; }

        [JsonProperty("channel_overrides")]
        public List<ChannelOverrides> ChannelOverrides { get; set; }
    }

    public class ChannelOverrides
    {
        [JsonProperty("message_notifications")]
        public NotificationSettingLevel NotificationLevel { get; set; }

        [JsonProperty("muted")]
        public bool IsMuted { get; set; }

        [JsonProperty("channel_id")]
        public ulong ChannelId { get; set; }
    }

    public enum NotificationSettingLevel
    {
        AllMessages = 0,
        Mentions = 1,
        Nothing = 2,
        Parent = 3
    }
}
