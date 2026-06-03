using System.ComponentModel.DataAnnotations;

namespace UrbanDrive.Models
{
    public class Allocation
    {
        [Key]
        public int AllocationId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [Required]
        public int DriverId { get; set; }

        [Required]
        public int ApprovedBy { get; set; }

        public DateTime ApprovalDate { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string AllocationStatus { get; set; } = "Assigned";

        [Display(Name = "Notes for Driver")]
        public string? NotesForDriver { get; set; }

        [Display(Name = "Notes for Passenger")]
        public string? NotesForPassenger { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Booking Booking { get; set; }
        public virtual Vehicle Vehicle { get; set; }
        public virtual Driver Driver { get; set; }
        public virtual User Approver { get; set; }
        public virtual TripReport TripReport { get; set; }
        public virtual ICollection<FuelRecord> FuelRecords { get; set; } = new List<FuelRecord>();
    }
}