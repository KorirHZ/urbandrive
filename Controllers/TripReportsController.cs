using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UrbanDrive.Data;
using UrbanDrive.Models;

namespace UrbanDrive.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TripReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TripReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: All Trip Reports
        public async Task<IActionResult> Index()
        {
            var tripReports = await _context.TripReports
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Booking)
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Vehicle)
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Driver)
                        .ThenInclude(d => d.User)
                .Include(t => t.Submitter)
                .OrderByDescending(t => t.EndTime)
                .ToListAsync();

            return View(tripReports);
        }

        // GET: Trip Report Details
        public async Task<IActionResult> Details(int id)
        {
            var tripReport = await _context.TripReports
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Booking)
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Vehicle)
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Driver)
                        .ThenInclude(d => d.User)
                .Include(t => t.Submitter)
                .Include(t => t.Reviewer)
                .FirstOrDefaultAsync(t => t.TripReportId == id);

            if (tripReport == null)
            {
                return NotFound();
            }

            return View(tripReport);
        }

        // POST: Review Trip Report
        [HttpPost]
        public async Task<IActionResult> Review(int id, string adminNotes)
        {
            var tripReport = await _context.TripReports.FindAsync(id);
            if (tripReport == null)
            {
                return NotFound();
            }

            tripReport.AdminNotes = adminNotes;
            tripReport.ReviewedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            tripReport.ReviewedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Trip report reviewed successfully";
            return RedirectToAction(nameof(Index));
        }

        // GET: Trip Summary Report
        public async Task<IActionResult> Summary(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.TripReports
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Booking)
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Vehicle)
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Driver)
                        .ThenInclude(d => d.User)
                .Where(t => t.ReportStatus == "Completed")
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.EndTime >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(t => t.EndTime <= toDate.Value);
            }

            var tripReports = await query.OrderByDescending(t => t.EndTime).ToListAsync();

            ViewBag.TotalDistance = tripReports.Sum(t => t.TotalDistance ?? 0);
            ViewBag.TotalFuelUsed = tripReports.Sum(t => t.ActualFuelUsed ?? 0);
            ViewBag.TotalTrips = tripReports.Count;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(tripReports);
        }
    }
}