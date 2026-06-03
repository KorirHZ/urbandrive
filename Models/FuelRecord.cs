using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Display(Name = "Fuel Liters")]
        [Range(0.01, 1000)]
        public decimal FuelLiters { get; set; }

        [Required]
        [Display(Name = "Fuel Cost")]
        [Range(0.01, 100000)]
        public decimal FuelCost { get; set; }

        [Display(Name = "Cost Per Liter")]
        public decimal CostPerLiter { get; set; }

        [Required]
        [Display(Name = "Current Mileage")]
        public int CurrentMileage { get; set; }

        [Required]
        [Display(Name = "Date Issued")]
        [DataType(DataType.DateTime)]
        public DateTime DateIssued { get; set; } = DateTime.Now;

        
        public int? IssuedBy { get; set; }

        [StringLength(50)]
        [Display(Name = "Receipt Number")]
        public string? ReceiptNumber { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        public virtual Vehicle Vehicle { get; set; }
        public virtual Driver Driver { get; set; }
        public virtual Allocation Allocation { get; set; }

        // public virtual User Issuer { get; set; }
    }
}