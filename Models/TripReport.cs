using System.ComponentModel.DataAnnotations;

namespace UrbanDrive.Models
{
    public class TripReport
    {
        [Key]
        public int TripReportId { get; set; }

        [Required]
        public int AllocationId { get; set; }

        [Display(Name = "Start Mileage")]
        public int? StartMileage { get; set; }

        [Display(Name = "End Mileage")]
        public int? EndMileage { get; set; }

        [Display(Name = "Total Distance")]
        public int? TotalDistance { get; set; }

        [Display(Name = "Actual Fuel Used")]
        [Range(0, 1000)]
        public decimal? ActualFuelUsed { get; set; }

        [Display(Name = "Start Time")]
        [DataType(DataType.DateTime)]
        public DateTime? StartTime { get; set; }

        [Display(Name = "End Time")]
        [DataType(DataType.DateTime)]
        public DateTime? EndTime { get; set; }

        [Required]
        [StringLength(20)]
        public string ReportStatus { get; set; } = "NotStarted";

        [Display(Name = "Driver Notes")]
        public string? DriverNotes { get; set; }

        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }

        public int? SubmittedBy { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public int? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        // Navigation properties
        public virtual Allocation Allocation { get; set; }
        public virtual User Submitter { get; set; }
        public virtual User Reviewer { get; set; }
    }
}