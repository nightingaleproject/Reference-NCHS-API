using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace messaging.Models
{
    [Index(nameof(CreatedDate))]
    public class OutgoingMessageItem : BaseEntity
    {
        public long Id { get; set; }
        [Required]
        public string Message { get; set; }
        [Required]
        public string MessageType { get; set; }
        [Required]
        public string MessageId { get; set; }
        [Column(TypeName = "CHAR")]
        [MaxLength(2)]
        [Required]
        public string JurisdictionId { get; set; }
        public uint? EventYear { get; set;}
        public uint? CertificateNumber {get; set;}
        public string EventType {get; set;}
    }
}
