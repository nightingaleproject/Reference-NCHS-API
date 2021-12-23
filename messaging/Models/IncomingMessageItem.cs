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
        [Column(TypeName = "CHAR")]
        [MaxLength(3)]
        public string Source { get; set; }
        [Column(TypeName = "CHAR")]
        [MaxLength(10)]
        [Required]
        public string ProcessedStatus { get; set; } = "QUEUED";
        [Column(TypeName = "CHAR")]
        [MaxLength(2)]
        [Required]
        public string JurisdictionId { get; set; }
    }
}
