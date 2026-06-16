using System.ComponentModel.DataAnnotations;

namespace UrbanDrive.Models
{
    public class FuelPriceSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public decimal PricePerLiter { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public int UpdatedBy { get; set; }
    }
}