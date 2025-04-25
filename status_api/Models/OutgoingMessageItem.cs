using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace status_api.Models
{
    [Index(nameof(CreatedDate))]
    public class OutgoingMessageItem : BaseEntity
    {
        // read-only attributes
        public long Id { get; }
        [Required]
        public string Message { get; }
        [Required]
        public string MessageType { get; }
        [Required]
        public string MessageId { get; }
        [Column(TypeName = "CHAR")]
        [MaxLength(2)]
        [Required]
        public string JurisdictionId { get; }
        public uint? EventYear { get; }
        [Column(TypeName = "CHAR")]
        [MaxLength(6)]
        public string CertificateNumber { get; }
        [Column(TypeName = "CHAR")]
        [MaxLength(3)]
        public string EventType { get; }

        public DateTime? RetrievedAt { get; }
    }
}
