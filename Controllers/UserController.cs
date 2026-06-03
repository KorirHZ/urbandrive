using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UrbanDrive.Data;
using UrbanDrive.Models;
using UrbanDrive.Services;

namespace UrbanDrive.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public UserController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: User Dashboard (Updated with ViewModel)
        public async Task<IActionResult> Dashboard()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            ViewBag.UserName = user.FullName;

            var allBookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .ToListAsync();

            var viewModel = new UserDashboardViewModel
            {
                TotalBookings = allBookings.Count,
                PendingBookings = allBookings.Count(b => b.Status == "Pending"),
                CompletedBookings = allBookings.Count(b => b.Status == "Completed"),
                RecentBookings = allBookings.OrderByDescending(b => b.CreatedAt).Take(10).ToList()
            };

            return View(viewModel);
        }

        // GET: Create Booking (Returns the booking form view)
        public IActionResult CreateBooking()
        {
            return View();
        }

        // POST: Create Booking (Traditional form submission)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking(Booking model)
        {
            if (ModelState.IsValid)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                model.UserId = userId;
                model.Status = "Pending";
                model.CreatedAt = DateTime.Now;

                _context.Bookings.Add(model);
                await _context.SaveChangesAsync();

                // Send email to Admin
                var adminEmail = "swiftbuyventuresceo@gmail.com";
                var placeholders = new Dictionary<string, string>
                {
                    { "BookingId", model.BookingId.ToString() },
                    { "RequesterName", User.FindFirstValue(ClaimTypes.Name) },
                    { "Destination", model.Destination },
                    { "StartDate", model.StartDate.ToString("MMM dd, yyyy") },
                    { "Purpose", model.Purpose },
                    { "ApprovalLink", Url.Action("PendingApproval", "Bookings", null, Request.Scheme) }
                };
                await _emailService.SendEmailWithTemplateAsync(adminEmail, "Admin", "NewBooking", placeholders);

                TempData["SuccessMessage"] = "Booking request submitted successfully!";
                return RedirectToAction("MyBookings");
            }
            return View(model);
        }

        // POST: Create Booking (AJAX - for real-time dashboard updates)
        [HttpPost]
        public async Task<IActionResult> CreateBookingAjax([FromBody] Booking model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid booking data" });
                }

                // Validate required fields
                if (string.IsNullOrEmpty(model.Destination))
                {
                    return Json(new { success = false, message = "Destination is required" });
                }

                if (model.StartDate == default(DateTime))
                {
                    return Json(new { success = false, message = "Start date is required" });
                }

                if (string.IsNullOrEmpty(model.Purpose))
                {
                    return Json(new { success = false, message = "Purpose is required" });
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                model.UserId = userId;
                model.Status = "Pending";
                model.CreatedAt = DateTime.Now;

                // Handle EndDate if not provided
                if (model.EndDate == default(DateTime))
                {
                    model.EndDate = null;
                }

                _context.Bookings.Add(model);
                await _context.SaveChangesAsync();

                // Send email to Admin (background - don't wait for it)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var adminEmail = "swiftbuyventuresceo@gmail.com";
                        var placeholders = new Dictionary<string, string>
                        {
                            { "BookingId", model.BookingId.ToString() },
                            { "RequesterName", User.FindFirstValue(ClaimTypes.Name) },
                            { "Destination", model.Destination },
                            { "StartDate", model.StartDate.ToString("MMM dd, yyyy") },
                            { "Purpose", model.Purpose },
                            { "ApprovalLink", Url.Action("PendingApproval", "Bookings", null, Request.Scheme) }
                        };
                        await _emailService.SendEmailWithTemplateAsync(adminEmail, "Admin", "NewBooking", placeholders);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email error: {ex.Message}");
                    }
                });

                // Get updated counts
                var allBookings = await _context.Bookings
                    .Where(b => b.UserId == userId)
                    .ToListAsync();

                var newBooking = new
                {
                    bookingId = model.BookingId,
                    destination = model.Destination,
                    startDate = model.StartDate.ToString("MMM dd, yyyy 'at' hh:mm tt"),
                    purpose = model.Purpose.Length > 30 ? model.Purpose.Substring(0, 30) + "..." : model.Purpose,
                    status = model.Status
                };

                return Json(new
                {
                    success = true,
                    booking = newBooking,
                    newTotal = allBookings.Count,
                    newPending = allBookings.Count(b => b.Status == "Pending"),
                    newCompleted = allBookings.Count(b => b.Status == "Completed")
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateBookingAjax Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // GET: My Bookings (Traditional view)
        public async Task<IActionResult> MyBookings()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var bookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // GET: Booking Details (Traditional view)
        public async Task<IActionResult> BookingDetails(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var booking = await _context.Bookings
                .Include(b => b.Allocation)
                    .ThenInclude(a => a.Driver)
                        .ThenInclude(d => d.User)
                .Include(b => b.Allocation)
                    .ThenInclude(a => a.Vehicle)
                .FirstOrDefaultAsync(b => b.BookingId == id && b.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: Get Booking Details (AJAX - for modal)
        [HttpGet]
        public async Task<IActionResult> GetBookingDetails(int bookingId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);

            if (booking == null)
            {
                return Json(new { success = false, message = "Booking not found" });
            }

            var allocation = await _context.Allocations
                .Include(a => a.Driver)
                    .ThenInclude(d => d.User)
                .Include(a => a.Vehicle)
                .FirstOrDefaultAsync(a => a.BookingId == bookingId);

            var result = new
            {
                success = true,
                booking = new
                {
                    bookingId = booking.BookingId,
                    destination = booking.Destination,
                    pickupLocation = booking.PickupLocation,
                    startDate = booking.StartDate.ToString("MMM dd, yyyy 'at' hh:mm tt"),
                    endDate = booking.EndDate?.ToString("MMM dd, yyyy 'at' hh:mm tt") ?? "Not specified",
                    purpose = booking.Purpose,
                    numberOfPassengers = booking.NumberOfPassengers,
                    specialRequests = booking.SpecialRequests ?? "None",
                    status = booking.Status,
                    rejectionReason = booking.RejectionReason,
                    driverName = allocation?.Driver?.User?.FullName,
                    driverPhone = allocation?.Driver?.PhoneNumber,
                    driverEmail = allocation?.Driver?.Email,
                    vehicleModel = allocation?.Vehicle?.Model,
                    vehicleReg = allocation?.Vehicle?.RegistrationNumber,
                    vehicleNotes = allocation?.NotesForPassenger
                }
            };

            return Json(result);
        }

        // POST: Cancel Booking (Traditional form submission)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == id && b.UserId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == "Pending")
            {
                booking.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Booking cancelled successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Cannot cancel this booking.";
            }

            return RedirectToAction("MyBookings");
        }

        // POST: Cancel Booking (AJAX - for real-time updates)
        [HttpPost]
        public async Task<IActionResult> CancelBookingAjax(int bookingId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);

            if (booking == null)
            {
                return Json(new { success = false, message = "Booking not found" });
            }

            if (booking.Status != "Pending")
            {
                return Json(new { success = false, message = "Cannot cancel this booking. Only pending bookings can be cancelled." });
            }

            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();

            var allBookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .ToListAsync();

            return Json(new
            {
                success = true,
                message = "Booking cancelled successfully",
                newTotal = allBookings.Count,
                newPending = allBookings.Count(b => b.Status == "Pending"),
                newCompleted = allBookings.Count(b => b.Status == "Completed")
            });
        }
    }
}