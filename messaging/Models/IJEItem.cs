using System;
using System.Text.Json.Serialization;

namespace messaging.Models
{
    public class IJEItem
    {
        public long Id { get; set; }
        public string MessageId { get; set; }
        public string IJE { get; set; }
    }
}
