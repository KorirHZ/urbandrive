using System.Collections.Generic;

namespace UrbanDrive.Models
{
    public class DriverManagementViewModel
    {
        public List<DriverWithUser> Drivers { get; set; }
        public int TotalDrivers { get; set; }
        public int AvailableDrivers { get; set; }
        public int UnavailableDrivers { get; set; }
    }

    public class DriverWithUser
    {
        public int DriverId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime? HireDate { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
    }
}