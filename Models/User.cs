using System.ComponentModel.DataAnnotations;

namespace UrbanDrive.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpiry { get; set; }

        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "User";

        public bool IsActive { get; set; } = true;

        public bool IsEmailVerified { get; set; } = false;

        public string? EmailVerificationToken { get; set; }

        public bool MustChangePassword { get; set; } = false;

        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        public DateTime? LastPasswordChange { get; set; }

        public int? CreatedBy { get; set; }

        // Navigation properties
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Allocation> AllocationsApproved { get; set; } = new List<Allocation>();
        public virtual ICollection<FuelRecord> FuelRecordsIssued { get; set; } = new List<FuelRecord>();
        public virtual ICollection<TripReport> TripReportsSubmitted { get; set; } = new List<TripReport>();
        public virtual ICollection<TripReport> TripReportsReviewed { get; set; } = new List<TripReport>();
    }
}