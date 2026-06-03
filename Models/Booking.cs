using System;
using System.ComponentModel.DataAnnotations;
namespace UrbanDrive.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Destination is required")]
        [StringLength(200)]
        public string Destination { get; set; }

        [StringLength(200)]
        [Display(Name = "Pickup Location")]
        public string? PickupLocation { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        [DataType(DataType.DateTime)]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Purpose is required")]
        public string Purpose { get; set; }

        [Display(Name = "Number of Passengers")]
        [Range(1, 50)]
        public int NumberOfPassengers { get; set; } = 1;

        [Display(Name = "Special Requests")]
        public string? SpecialRequests { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Allocation Allocation { get; set; }
    }
}