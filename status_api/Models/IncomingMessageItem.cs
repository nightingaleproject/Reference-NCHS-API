using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace status_api.Models
{
    public class IncomingMessageItem : BaseEntity
    {
        // All attributes are read-only
        public long Id { get; }
        [JsonIgnore]
        public string Message { get; }
        [Required]
        public string MessageId { get; }
        [Required]
        public string MessageType { get; }
        [Column(TypeName = "CHAR")]
        [MaxLength(3)]
        [Required]
        public string Source { get; } = "SAM";
        [Column(TypeName = "CHAR")]
        [MaxLength(10)]
        [Required]
        public string ProcessedStatus { get; } = "QUEUED";
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
    }
}
