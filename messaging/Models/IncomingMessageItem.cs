using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace messaging.Models
{
    public class IncomingMessageItem : BaseEntity
    {
        public long Id { get; set; }
        [JsonIgnore]
        public string Message { get; set; }
        [Required]
        public string MessageId { get; set; }
        [Required]
        public string MessageType { get; set; }
        [Column(TypeName = "CHAR")]
        [MaxLength(3)]
        [Required]
        public string Source { get; set; } = "SAM";
        [Column(TypeName = "CHAR")]
        [MaxLength(10)]
        [Required]
        public string ProcessedStatus { get; set; } = "QUEUED";
        [Column(TypeName = "CHAR")]
        [MaxLength(2)]
        [Required]
        public string JurisdictionId { get; set; }
        public uint? EventYear { get; set;}
        [Column(TypeName = "CHAR")]
        [MaxLength(6)]
        public string CertificateNumber {get; set;}
        [Column(TypeName = "CHAR")]
        [MaxLength(3)]
        public string EventType {get; set;}
        [Column(TypeName = "CHAR")]
        [MaxLength(20)]
        public string IGVersion { get; set; }
    }
}
