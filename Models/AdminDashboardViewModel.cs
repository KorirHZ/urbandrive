using System.Collections.Generic;

namespace UrbanDrive.Models
{
    public class AdminDashboardViewModel
    {
        // Stats
        public int TotalVehicles { get; set; }
        public int AvailableVehicles { get; set; }
        public int TotalDrivers { get; set; }
        public int AvailableDrivers { get; set; }
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ApprovedBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int TotalUsers { get; set; }
        public int TotalDriversCount { get; set; }

        // Lists
        public List<Vehicle> Vehicles { get; set; }
        public List<Driver> Drivers { get; set; }
        public List<Booking> PendingApprovals { get; set; }
        public List<Booking> RecentBookings { get; set; }
        public List<User> AllUsers { get; set; }
        public List<FuelRecord> FuelRecords { get; set; }
        public List<TripReport> TripReports { get; set; }
    }
}