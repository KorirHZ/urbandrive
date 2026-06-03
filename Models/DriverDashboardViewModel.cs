using System.Collections.Generic;

namespace UrbanDrive.Models
{
    public class DriverDashboardViewModel
    {
        public bool IsAvailable { get; set; }
        public List<Allocation> AssignedTrips { get; set; }
        public List<TripReport> RecentTrips { get; set; }
        public int CompletedTripsCount { get; set; }
        public int TotalDistance { get; set; }
    }
}