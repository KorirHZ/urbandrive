using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UrbanDrive.Data;
using UrbanDrive.Models;

namespace UrbanDrive.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: All Bookings
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(bookings);
        }

        // GET: Pending Approvals
        public async Task<IActionResult> PendingApproval()
        {
            var pendingBookings = await _context.Bookings
                .Include(b => b.User)
                .Where(b => b.Status == "Pending")
                .OrderBy(b => b.StartDate)
                .ToListAsync();
            return View(pendingBookings);
        }

        // GET: Booking Details
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Allocation)
                    .ThenInclude(a => a.Vehicle)
                .Include(b => b.Allocation)
                    .ThenInclude(a => a.Driver)
                        .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }
            return View(booking);
        }

        // POST: Reject Booking
        [HttpPost]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = "Rejected";
            booking.RejectionReason = rejectionReason;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Booking #{id} has been rejected";
            return RedirectToAction(nameof(PendingApproval));
        }
    }
}