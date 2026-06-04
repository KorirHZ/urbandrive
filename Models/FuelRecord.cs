using System.ComponentModel.DataAnnotations;

namespace UrbanDrive.Models
{
    public class FuelRecord
    {
        [Key]
        public int FuelRecordId { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [Required]
        public int DriverId { get; set; }

        public int? AllocationId { get; set; }

        [Required]
        [Range(0.01, 1000)]
        public decimal FuelLiters { get; set; }

        public decimal FuelCost { get; set; }

        public decimal CostPerLiter { get; set; }

        [Required]
        public int CurrentMileage { get; set; }

        [Required]
        public DateTime DateIssued { get; set; } = DateTime.Now;

        public int? IssuedBy { get; set; }

        [StringLength(50)]
        public string? ReceiptNumber { get; set; }

        public string? Notes { get; set; }

        // Navigation properties - KEEP THESE
        public virtual Vehicle Vehicle { get; set; }
        public virtual Driver Driver { get; set; }
        public virtual Allocation Allocation { get; set; }
        //public virtual User Issuer { get; set; }
    }
}