using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace messaging.Models
{
    [Index(nameof(MessageId))]
    [Index(nameof(NCHSIdentifier))]
    public class IncomingMessageLog : BaseEntity
    {
        public long Id { get; set; }
        [Required]
        public string MessageId { get; set; }
        public string NCHSIdentifier { get; set; }
        public string StateAuxiliaryIdentifier { get; set; }
        public DateTimeOffset? MessageTimestamp { get; set; }
        [Column(TypeName = "CHAR")]
        [MaxLength(2)]
        [Required]
        public string JurisdictionId { get; set; }
    }
}
