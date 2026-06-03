using System.ComponentModel.DataAnnotations;

namespace UrbanDrive.Models
{
    public class Driver
    {
        [Key]
        public int DriverId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "License number is required")]
        [StringLength(50)]
        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; }

        [Display(Name = "License Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? LicenseExpiryDate { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Hire Date")]
        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
        public virtual ICollection<FuelRecord> FuelRecords { get; set; } = new List<FuelRecord>();
    }
}