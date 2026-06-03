using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UrbanDrive.Data;
using UrbanDrive.Models;

namespace UrbanDrive.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DriverController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== DASHBOARD ====================

        // GET: Driver Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
            {
                return NotFound();
            }

            // Get assigned trips (not completed)
            var assignedTrips = await _context.Allocations
                .Include(a => a.Booking)
                    .ThenInclude(b => b.User)
                .Include(a => a.Vehicle)
                .Include(a => a.TripReport)
                .Where(a => a.DriverId == driver.DriverId && a.AllocationStatus != "Completed")
                .OrderBy(a => a.Booking.StartDate)
                .ToListAsync();

            // Get completed trips for history
            var completedTrips = await _context.TripReports
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Booking)
                        .ThenInclude(b => b.User)
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Vehicle)
                .Where(t => t.Allocation.DriverId == driver.DriverId && t.ReportStatus == "Completed")
                .OrderByDescending(t => t.EndTime)
                .Take(10)
                .ToListAsync();

            // Calculate total distance
            var totalDistance = completedTrips.Sum(t => t.TotalDistance ?? 0);

            var viewModel = new DriverDashboardViewModel
            {
                IsAvailable = driver.IsAvailable,
                AssignedTrips = assignedTrips,
                RecentTrips = completedTrips,
                CompletedTripsCount = completedTrips.Count,
                TotalDistance = totalDistance
            };

            return View(viewModel);
        }

        // ==================== AVAILABILITY ====================

        // POST: Toggle Driver Availability
        [HttpPost]
        public async Task<IActionResult> ToggleAvailability([FromBody] ToggleAvailabilityRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

                if (driver == null)
                    return Json(new { success = false, message = "Driver not found" });

                driver.IsAvailable = request.IsAvailable;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== START TRIP ====================

        // POST: Start Trip
        [HttpPost]
        public async Task<IActionResult> StartTrip([FromBody] StartTripRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

                if (driver == null)
                    return Json(new { success = false, message = "Driver not found" });

                var allocation = await _context.Allocations
                    .Include(a => a.Vehicle)
                    .FirstOrDefaultAsync(a => a.AllocationId == request.AllocationId && a.DriverId == driver.DriverId);

                if (allocation == null)
                    return Json(new { success = false, message = "Allocation not found" });

                if (allocation.AllocationStatus != "Assigned")
                    return Json(new { success = false, message = "Trip already started or completed" });

                // Check if trip report exists
                var tripReport = await _context.TripReports.FirstOrDefaultAsync(t => t.AllocationId == request.AllocationId);

                if (tripReport == null)
                {
                    tripReport = new TripReport
                    {
                        AllocationId = request.AllocationId,
                        StartMileage = request.StartMileage,
                        StartTime = DateTime.Now,
                        ReportStatus = "InProgress"
                    };
                    _context.TripReports.Add(tripReport);
                }
                else
                {
                    tripReport.StartMileage = request.StartMileage;
                    tripReport.StartTime = DateTime.Now;
                    tripReport.ReportStatus = "InProgress";
                }

                allocation.AllocationStatus = "InProgress";
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== COMPLETE TRIP ====================

        // POST: Complete Trip (WITHOUT foreign key issues)
        [HttpPost]
        public async Task<IActionResult> CompleteTrip([FromBody] CompleteTripRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var driver = await _context.Drivers
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.UserId == userId);

                if (driver == null)
                    return Json(new { success = false, message = "Driver not found" });

                var allocation = await _context.Allocations
                    .Include(a => a.Vehicle)
                    .Include(a => a.Booking)
                    .FirstOrDefaultAsync(a => a.AllocationId == request.AllocationId && a.DriverId == driver.DriverId);

                if (allocation == null)
                    return Json(new { success = false, message = "Allocation not found" });

                if (allocation.AllocationStatus != "InProgress")
                    return Json(new { success = false, message = "No active trip to complete" });

                var tripReport = await _context.TripReports
                    .FirstOrDefaultAsync(t => t.AllocationId == request.AllocationId);

                if (tripReport == null)
                {
                    tripReport = new TripReport
                    {
                        AllocationId = request.AllocationId,
                        StartMileage = 0,
                        ReportStatus = "Completed"
                    };
                    _context.TripReports.Add(tripReport);
                }

                var totalDistance = request.EndMileage - (tripReport.StartMileage ?? 0);

                tripReport.EndMileage = request.EndMileage;
                tripReport.TotalDistance = totalDistance > 0 ? totalDistance : 0;
                tripReport.ActualFuelUsed = request.ActualFuelUsed;
                tripReport.DriverNotes = request.DriverNotes;
                tripReport.EndTime = DateTime.Now;
                tripReport.ReportStatus = "Completed";
                tripReport.SubmittedBy = userId;
                tripReport.SubmittedAt = DateTime.Now;

                allocation.AllocationStatus = "Completed";

                var vehicle = await _context.Vehicles.FindAsync(allocation.VehicleId);
                if (vehicle != null)
                {
                    vehicle.CurrentMileage = request.EndMileage;
                    vehicle.UpdatedAt = DateTime.Now;
                }

                var booking = await _context.Bookings.FindAsync(allocation.BookingId);
                if (booking != null)
                {
                    booking.Status = "Completed";
                    booking.UpdatedAt = DateTime.Now;
                }

                // ✅ Create Fuel Record - NO FOREIGN KEY CONSTRAINT
                var fuelRecord = new FuelRecord
                {
                    VehicleId = allocation.VehicleId,
                    DriverId = driver.DriverId,
                    AllocationId = request.AllocationId,
                    FuelLiters = request.ActualFuelUsed,
                    FuelCost = 0,
                    CostPerLiter = 0,
                    CurrentMileage = request.EndMileage,
                    DateIssued = DateTime.Now,
                    IssuedBy = null,  
                    ReceiptNumber = null,
                    Notes = $"Auto-generated from trip #{allocation.BookingId} completion"
                };
                _context.FuelRecords.Add(fuelRecord);

                await _context.SaveChangesAsync();

                return Json(new { success = true, totalDistance = totalDistance, fuelUsed = request.ActualFuelUsed });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }
        // ==================== TRIP HISTORY ====================

        // GET: Trip History Page
        public async Task<IActionResult> TripHistory()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
                return NotFound();

            var completedTrips = await _context.TripReports
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Booking)
                        .ThenInclude(b => b.User)
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Vehicle)
                .Where(t => t.Allocation.DriverId == driver.DriverId && t.ReportStatus == "Completed")
                .OrderByDescending(t => t.EndTime)
                .ToListAsync();

            return View(completedTrips);
        }

        // GET: Get Trip Report Details (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetTripReport(int tripReportId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
                return Json(new { success = false });

            var tripReport = await _context.TripReports
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Booking)
                        .ThenInclude(b => b.User)
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Vehicle)
                .FirstOrDefaultAsync(t => t.TripReportId == tripReportId && t.Allocation.DriverId == driver.DriverId);

            if (tripReport == null)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                bookingId = tripReport.Allocation.BookingId,
                destination = tripReport.Allocation.Booking.Destination,
                startMileage = tripReport.StartMileage,
                endMileage = tripReport.EndMileage,
                fuelUsed = tripReport.ActualFuelUsed,
                driverNotes = tripReport.DriverNotes,
                endTime = tripReport.EndTime?.ToString("MMM dd, yyyy 'at' hh:mm tt"),
                passengerName = tripReport.Allocation.Booking.User?.FullName,
                passengerPhone = tripReport.Allocation.Booking.User?.PhoneNumber,
                vehicleModel = tripReport.Allocation.Vehicle?.Model,
                vehicleReg = tripReport.Allocation.Vehicle?.RegistrationNumber
            });
        }
    }

    // ==================== REQUEST CLASSES ====================

    public class ToggleAvailabilityRequest
    {
        public bool IsAvailable { get; set; }
    }

    public class StartTripRequest
    {
        public int AllocationId { get; set; }
        public int StartMileage { get; set; }
    }

    public class CompleteTripRequest
    {
        public int AllocationId { get; set; }
        public int EndMileage { get; set; }
        public decimal ActualFuelUsed { get; set; }
        public string DriverNotes { get; set; }
    }
}