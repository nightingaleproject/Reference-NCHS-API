using System;

namespace NVSSMessaging.Models
{
    public class IncomingMessageLog : BaseEntity
    {
        public long Id { get; set; }
        public string MessageId { get; set; }
        public uint? CertificateNumber { get; set; }
        public string StateAuxiliaryIdentifier { get; set; }
        public DateTimeOffset? MessageTimestamp { get; set; }
    }
}
