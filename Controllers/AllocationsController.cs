using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UrbanDrive.Data;
using UrbanDrive.Models;
using UrbanDrive.Services;

namespace UrbanDrive.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AllocationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AllocationsController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Allocate Booking
        public async Task<IActionResult> Allocate(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null || booking.Status != "Pending")
            {
                return NotFound();
            }

            ViewBag.Booking = booking;
            ViewBag.Vehicles = await _context.Vehicles
                .Where(v => v.Status == "Available")
                .ToListAsync();
            ViewBag.Drivers = await _context.Drivers
                .Include(d => d.User)
                .Where(d => d.IsAvailable)
                .ToListAsync();

            return View();
        }

        // POST: Allocate Booking
        [HttpPost]
        public async Task<IActionResult> Allocate(int bookingId, int vehicleId, int driverId, string notesForDriver, string notesForPassenger)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null || booking.Status != "Pending")
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            var driver = await _context.Drivers
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DriverId == driverId);

            if (vehicle == null || driver == null)
            {
                TempData["ErrorMessage"] = "Invalid vehicle or driver selection";
                return RedirectToAction("Allocate", new { bookingId });
            }

            // Create allocation
            var allocation = new Allocation
            {
                BookingId = bookingId,
                VehicleId = vehicleId,
                DriverId = driverId,
                ApprovedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                ApprovalDate = DateTime.Now,
                AllocationStatus = "Assigned",
                NotesForDriver = notesForDriver,
                NotesForPassenger = notesForPassenger,
                CreatedAt = DateTime.Now
            };

            _context.Allocations.Add(allocation);

            // Update booking status
            booking.Status = "Approved";
            booking.UpdatedAt = DateTime.Now;

            // Update vehicle status
            vehicle.Status = "InUse";

            // Update driver availability
            driver.IsAvailable = false;

            await _context.SaveChangesAsync();

            // Send email to Driver
            var driverDashboardLink = Url.Action("Dashboard", "Driver", null, Request.Scheme);
            var driverPlaceholders = new Dictionary<string, string>
            {
                { "DriverName", driver.User.FullName },
                { "PassengerName", booking.User.FullName },
                { "PassengerPhone", booking.User.PhoneNumber ?? "Not provided" },
                { "Destination", booking.Destination },
                { "PickupDate", booking.StartDate.ToString("MMM dd, yyyy 'at' hh:mm tt") },
                { "VehicleReg", vehicle.RegistrationNumber },
                { "DashboardLink", driverDashboardLink }
            };
            await _emailService.SendEmailWithTemplateAsync(driver.User.Email, driver.User.FullName, "DriverAssignment", driverPlaceholders);

            // Send email to Passenger
            var trackingLink = Url.Action("MyBookings", "User", null, Request.Scheme);
            var passengerPlaceholders = new Dictionary<string, string>
            {
                { "PassengerName", booking.User.FullName },
                { "DriverName", driver.User.FullName },
                { "DriverPhone", driver.PhoneNumber ?? "Not provided" },
                { "VehicleReg", vehicle.RegistrationNumber },
                { "PickupDate", booking.StartDate.ToString("MMM dd, yyyy 'at' hh:mm tt") },
                { "TrackingLink", trackingLink }
            };
            await _emailService.SendEmailWithTemplateAsync(booking.User.Email, booking.User.FullName, "BookingConfirmation", passengerPlaceholders);

            TempData["SuccessMessage"] = $"Booking #{bookingId} has been approved and allocated successfully!";
            return RedirectToAction("PendingApproval", "Bookings");
        }

        // GET: All Allocations
        public async Task<IActionResult> Index()
        {
            var allocations = await _context.Allocations
                .Include(a => a.Booking)
                .Include(a => a.Vehicle)
                .Include(a => a.Driver)
                    .ThenInclude(d => d.User)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(allocations);
        }
    }
}