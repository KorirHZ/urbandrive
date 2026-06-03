using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UrbanDrive.Data;
using UrbanDrive.Models;

namespace UrbanDrive.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FuelRecordsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FuelRecordsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: All Fuel Records
        public async Task<IActionResult> Index()
        {
            var fuelRecords = await _context.FuelRecords
                .Include(f => f.Vehicle)
                .Include(f => f.Driver)
                    .ThenInclude(d => d.User)
                //.Include(f => f.Issuer)
                .OrderByDescending(f => f.DateIssued)
                .ToListAsync();
            return View(fuelRecords);
        }

        // GET: Create Fuel Record
        public async Task<IActionResult> Create()
        {
            ViewBag.Vehicles = await _context.Vehicles.ToListAsync();
            ViewBag.Drivers = await _context.Drivers.Include(d => d.User).ToListAsync();
            return View();
        }

        // POST: Create Fuel Record
        [HttpPost]
        public async Task<IActionResult> Create(FuelRecord fuelRecord)
        {
            if (ModelState.IsValid)
            {
                if (fuelRecord.FuelLiters > 0)
                {
                    fuelRecord.CostPerLiter = fuelRecord.FuelCost / fuelRecord.FuelLiters;
                }

                fuelRecord.IssuedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                fuelRecord.DateIssued = DateTime.Now;

                _context.FuelRecords.Add(fuelRecord);
                await _context.SaveChangesAsync();

                var vehicle = await _context.Vehicles.FindAsync(fuelRecord.VehicleId);
                if (vehicle != null && fuelRecord.CurrentMileage > vehicle.CurrentMileage)
                {
                    vehicle.CurrentMileage = fuelRecord.CurrentMileage;
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Fuel record added successfully";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Vehicles = await _context.Vehicles.ToListAsync();
            ViewBag.Drivers = await _context.Drivers.Include(d => d.User).ToListAsync();
            return View(fuelRecord);
        }

        // GET: Fuel Report
        public async Task<IActionResult> Report(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.FuelRecords
                .Include(f => f.Vehicle)
                .Include(f => f.Driver)
                    .ThenInclude(d => d.User)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(f => f.DateIssued >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(f => f.DateIssued <= toDate.Value);
            }

            var fuelRecords = await query.OrderByDescending(f => f.DateIssued).ToListAsync();

            ViewBag.TotalLiters = fuelRecords.Sum(f => f.FuelLiters);
            ViewBag.TotalCost = fuelRecords.Sum(f => f.FuelCost);
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(fuelRecords);
        }
    }
}