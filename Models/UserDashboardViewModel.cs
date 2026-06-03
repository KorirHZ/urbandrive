using System.Collections.Generic;

namespace UrbanDrive.Models
{
    public class UserDashboardViewModel
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public List<Booking> RecentBookings { get; set; }
    }
}