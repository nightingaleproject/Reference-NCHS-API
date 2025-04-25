using System;
using System.Text.Json.Serialization;

namespace status_api.Models
{
    public class IJEItem
    {
        public long Id { get; }
        public string MessageId { get; }
        public string IJE { get; }
    }
}
