using System.ComponentModel.DataAnnotations;
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
    }
}
