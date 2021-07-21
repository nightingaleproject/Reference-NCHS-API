using System;
using System.Text.Json.Serialization;

namespace NVSSMessaging.Models
{
    public class IncomingMessageItem : BaseEntity
    {
        public long Id { get; set; }
        [JsonIgnore]
        public string Message { get; set; }
        public string MessageId { get; set; }
    }
}
