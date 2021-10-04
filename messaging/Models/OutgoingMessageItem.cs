using System;
using System.Text.Json.Serialization;

namespace messaging.Models
{
    public class OutgoingMessageItem : BaseEntity
    {
        public long Id { get; set; }
        public string Message { get; set; }
        public string MessageId { get; set; }
    }
}
