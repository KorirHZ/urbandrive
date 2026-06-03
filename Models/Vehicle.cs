using System.ComponentModel.DataAnnotations;

namespace UrbanDrive.Models
{
    public class Vehicle
    {
        [Key]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Registration number is required")]
        [StringLength(20)]
        [Display(Name = "Registration Number")]
        public string RegistrationNumber { get; set; }

        [Required(ErrorMessage = "Model is required")]
        [StringLength(100)]
        public string Model { get; set; }

        [Required]
        [Range(1, 100)]
        public int Capacity { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Available";

        [Display(Name = "Last Service Date")]
        [DataType(DataType.Date)]
        public DateTime? LastServiceDate { get; set; }

        [Display(Name = "Next Service Due")]
        [DataType(DataType.Date)]
        public DateTime? NextServiceDue { get; set; }

        [Display(Name = "Current Mileage")]
        public int CurrentMileage { get; set; }

        [StringLength(20)]
        [Display(Name = "Fuel Type")]
        public string? FuelType { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
        public virtual ICollection<FuelRecord> FuelRecords { get; set; } = new List<FuelRecord>();
    }
}