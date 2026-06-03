using System.ComponentModel.DataAnnotations;

namespace UrbanDrive.Models
{
    public class EmailLog
    {
        [Key]
        public int EmailLogId { get; set; }

        [Required]
        [StringLength(150)]
        public string RecipientEmail { get; set; }

        [StringLength(100)]
        public string? RecipientName { get; set; }

        [StringLength(20)]
        public string? RecipientRole { get; set; }

        [StringLength(30)]
        public string EmailType { get; set; }

        [StringLength(200)]
        public string Subject { get; set; }

        public string? Body { get; set; }

        public int? RelatedBookingId { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string Status { get; set; } = "Sent";

        public string? ErrorMessage { get; set; }
    }
}