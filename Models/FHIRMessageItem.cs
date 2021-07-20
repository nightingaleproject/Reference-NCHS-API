using System;
using System.Text.Json.Serialization;

namespace NVSSMessaging.Models
{
    public class FHIRMessageItem
    {
        public long Id { get; set; }
        [JsonIgnore]
        public string Message { get; set; }
        public DateTime CreatedTimestamp { get; set; }
    }
}
