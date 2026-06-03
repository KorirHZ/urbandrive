using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UrbanDrive.Data;
using UrbanDrive.Models;

namespace UrbanDrive.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VehiclesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VehiclesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Vehicles
        public async Task<IActionResult> Index()
        {
            var vehicles = await _context.Vehicles.OrderByDescending(v => v.CreatedAt).ToListAsync();
            return View(vehicles);
        }

        // GET: Vehicles/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Vehicles/Create
        [HttpPost]
        public async Task<IActionResult> Create(Vehicle vehicle)
        {
            if (ModelState.IsValid)
            {
                vehicle.CreatedAt = DateTime.Now;
                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Vehicle added successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(vehicle);
        }

        // GET: Vehicles/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }
            return View(vehicle);
        }

        // POST: Vehicles/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Vehicle vehicle)
        {
            if (id != vehicle.VehicleId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                vehicle.UpdatedAt = DateTime.Now;
                _context.Update(vehicle);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Vehicle updated successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(vehicle);
        }

        // GET: Vehicles/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }
            return View(vehicle);
        }

        // POST: Vehicles/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle != null)
            {
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Vehicle deleted successfully";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Vehicles/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Allocations)
                    .ThenInclude(a => a.Booking)
                .Include(v => v.FuelRecords)
                .FirstOrDefaultAsync(v => v.VehicleId == id);

            if (vehicle == null)
            {
                return NotFound();
            }
            return View(vehicle);
        }
    }
}