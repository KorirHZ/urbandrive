using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UrbanDrive.Data;
using UrbanDrive.Models;
using UrbanDrive.Services;

namespace UrbanDrive.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;

        public AdminController(ApplicationDbContext context, IEmailService emailService, IUserService userService)
        {
            _context = context;
            _emailService = emailService;
            _userService = userService;
        }

        // ==================== DASHBOARD ====================

        // GET: Admin Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalVehicles = await _context.Vehicles.CountAsync(),
                AvailableVehicles = await _context.Vehicles.CountAsync(v => v.Status == "Available"),
                TotalDrivers = await _context.Drivers.CountAsync(),
                AvailableDrivers = await _context.Drivers.CountAsync(d => d.IsAvailable),
                TotalBookings = await _context.Bookings.CountAsync(),
                PendingBookings = await _context.Bookings.CountAsync(b => b.Status == "Pending"),
                ApprovedBookings = await _context.Bookings.CountAsync(b => b.Status == "Approved"),
                CompletedBookings = await _context.Bookings.CountAsync(b => b.Status == "Completed"),
                TotalUsers = await _context.Users.CountAsync(u => u.Role == "User"),
                PendingApprovals = await _context.Bookings.Include(b => b.User).Where(b => b.Status == "Pending").OrderBy(b => b.StartDate).ToListAsync(),
                RecentBookings = await _context.Bookings.Include(b => b.User).OrderByDescending(b => b.CreatedAt).Take(10).ToListAsync()
            };
            return View(viewModel);
        }

        // ==================== DRIVER MANAGEMENT ====================

        // GET: Driver Management Page
        public async Task<IActionResult> Drivers()
        {
            var drivers = await _context.Drivers
                .Include(d => d.User)
                .Select(d => new DriverWithUser
                {
                    DriverId = d.DriverId,
                    UserId = d.UserId,
                    FullName = d.User.FullName,
                    Email = d.User.Email,
                    PhoneNumber = d.PhoneNumber ?? d.User.PhoneNumber,
                    LicenseNumber = d.LicenseNumber,
                    LicenseExpiryDate = d.LicenseExpiryDate,
                    IsAvailable = d.IsAvailable,
                    HireDate = d.HireDate,
                    Notes = d.Notes,
                    IsActive = d.User.IsActive
                })
                .ToListAsync();

            var viewModel = new DriverManagementViewModel
            {
                Drivers = drivers,
                TotalDrivers = drivers.Count,
                AvailableDrivers = drivers.Count(d => d.IsAvailable),
                UnavailableDrivers = drivers.Count(d => !d.IsAvailable)
            };

            return View(viewModel);
        }

        // GET: Get Driver (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDriver(int driverId)
        {
            var driver = await _context.Drivers
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DriverId == driverId);

            if (driver == null)
                return Json(new { success = false, message = "Driver not found" });

            return Json(new
            {
                driverId = driver.DriverId,
                fullName = driver.User.FullName,
                email = driver.User.Email,
                phoneNumber = driver.PhoneNumber ?? driver.User.PhoneNumber,
                licenseNumber = driver.LicenseNumber,
                licenseExpiryDate = driver.LicenseExpiryDate,
                hireDate = driver.HireDate,
                isAvailable = driver.IsAvailable,
                notes = driver.Notes
            });
        }

        // POST: Create Driver
        [HttpPost]
        public async Task<IActionResult> CreateDriver([FromBody] CreateDriverRequest request)
        {
            try
            {
                // Check if email exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUser != null)
                    return Json(new { success = false, message = "Email already exists" });

                // Create User account
                var user = new User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Role = "Driver",
                    IsActive = true,
                    IsEmailVerified = true,
                    MustChangePassword = true,
                    RegisteredAt = DateTime.Now,
                    CreatedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
                };

                var tempPassword = _userService.GenerateToken().Substring(0, 10);
                await _userService.RegisterUserAsync(user, tempPassword);

                // Create Driver record
                var driver = new Driver
                {
                    UserId = user.UserId,
                    LicenseNumber = request.LicenseNumber,
                    LicenseExpiryDate = request.LicenseExpiryDate,
                    PhoneNumber = request.PhoneNumber,
                    Email = request.Email,
                    IsAvailable = request.IsAvailable,
                    HireDate = request.HireDate ?? DateTime.Now,
                    Notes = request.Notes,
                    CreatedAt = DateTime.Now
                };

                _context.Drivers.Add(driver);
                await _context.SaveChangesAsync();

                // Generate password reset token for welcome email
                var resetToken = _userService.GenerateToken();
                user.PasswordResetToken = resetToken;
                user.PasswordResetTokenExpiry = DateTime.Now.AddHours(48);
                await _context.SaveChangesAsync();

                // Send welcome email
                var resetLink = Url.Action("ResetPassword", "Account", new { email = user.Email, token = resetToken }, Request.Scheme);
                var placeholders = new Dictionary<string, string>
                {
                    { "FullName", user.FullName },
                    { "Role", "Driver" },
                    { "ResetLink", resetLink }
                };
                await _emailService.SendEmailWithTemplateAsync(user.Email, user.FullName, "Welcome", placeholders);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Update Driver
        [HttpPost]
        public async Task<IActionResult> UpdateDriver([FromBody] UpdateDriverRequest request)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.DriverId == request.DriverId);

                if (driver == null)
                    return Json(new { success = false, message = "Driver not found" });

                // Update User
                driver.User.FullName = request.FullName;
                driver.User.PhoneNumber = request.PhoneNumber;

                // Update Driver
                driver.LicenseNumber = request.LicenseNumber;
                driver.LicenseExpiryDate = request.LicenseExpiryDate;
                driver.PhoneNumber = request.PhoneNumber;
                driver.IsAvailable = request.IsAvailable;
                driver.Notes = request.Notes;
                driver.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Delete Driver
        [HttpPost]
        public async Task<IActionResult> DeleteDriver([FromBody] DeleteDriverRequest request)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.DriverId == request.DriverId);

                if (driver == null)
                    return Json(new { success = false, message = "Driver not found" });

                // Check if driver has active allocations
                var hasActiveAllocations = await _context.Allocations
                    .AnyAsync(a => a.DriverId == request.DriverId && a.AllocationStatus != "Completed");

                if (hasActiveAllocations)
                    return Json(new { success = false, message = "Cannot delete driver with active trips" });

                _context.Drivers.Remove(driver);
                _context.Users.Remove(driver.User);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Set Driver Password (Admin sets directly - NO EMAIL)
        [HttpPost]
        public async Task<IActionResult> SetDriverPassword([FromBody] SetDriverPasswordRequest request)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.DriverId == request.DriverId);

                if (driver == null)
                    return Json(new { success = false, message = "Driver not found" });

                // Validate password length
                if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
                    return Json(new { success = false, message = "Password must be at least 6 characters" });

                // Hash the new password
                driver.User.PasswordHash = _userService.HashPassword(request.NewPassword);
                driver.User.MustChangePassword = true; // Force password change on next login
                driver.User.LastPasswordChange = DateTime.Now;
                driver.User.PasswordResetToken = null;
                driver.User.PasswordResetTokenExpiry = null;

                await _context.SaveChangesAsync();

                // Log to console (for development)
                Console.WriteLine($"✅ Password set for driver: {driver.User.FullName} ({driver.User.Email})");
                Console.WriteLine($"   New Password: {request.NewPassword}");

                return Json(new { success = true, message = "Password set successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== VEHICLE MANAGEMENT ====================

        // GET: Vehicle Management Page
        public async Task<IActionResult> Vehicles()
        {
            var vehicles = await _context.Vehicles.OrderByDescending(v => v.CreatedAt).ToListAsync();
            return View("Vehicles/Index", vehicles); 
        }

        // GET: Create Vehicle Form
        public IActionResult CreateVehicle()
        {
            return View("Vehicles/Create");
        }

        // POST: Create Vehicle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVehicle(Vehicle vehicle)
        {
            if (ModelState.IsValid)
            {
                vehicle.CreatedAt = DateTime.Now;
                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Vehicle added successfully";
                return RedirectToAction(nameof(Vehicles));
            }
            return View("Vehicles/Create", vehicle);
        }

        // GET: Edit Vehicle Form
        public async Task<IActionResult> EditVehicle(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
                return NotFound();
            return View("Vehicles/Edit", vehicle);
        }

        // POST: Edit Vehicle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVehicle(int id, Vehicle vehicle)
        {
            if (id != vehicle.VehicleId)
                return NotFound();

            if (ModelState.IsValid)
            {
                vehicle.UpdatedAt = DateTime.Now;
                _context.Update(vehicle);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Vehicle updated successfully";
                return RedirectToAction(nameof(Vehicles));
            }
            return View("Vehicles/Edit", vehicle);
        }

        // GET: Delete Vehicle Confirmation
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
                return NotFound();
            return View("Vehicles/Delete", vehicle);
        }

        // POST: Delete Vehicle (SAME NAME)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVehicle(int id, string confirm)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle != null)
            {
                // Check if vehicle has active allocations
                var hasActiveAllocations = await _context.Allocations
                    .AnyAsync(a => a.VehicleId == id && a.AllocationStatus != "Completed");

                if (hasActiveAllocations)
                {
                    TempData["ErrorMessage"] = "Cannot delete vehicle with active trips";
                    return RedirectToAction(nameof(Vehicles));
                }

                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Vehicle deleted successfully";
            }
            return RedirectToAction(nameof(Vehicles));
        }

        // GET: Vehicle Details
        public async Task<IActionResult> VehicleDetails(int id)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Allocations)
                    .ThenInclude(a => a.Booking)
                .Include(v => v.FuelRecords)
                .FirstOrDefaultAsync(v => v.VehicleId == id);

            if (vehicle == null)
                return NotFound();
            return View("Vehicles/Details", vehicle);
        }
        // GET: Vehicle Details (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetVehicleDetails(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
                return Json(new { success = false });

            return Json(new
            {
                vehicleId = vehicle.VehicleId,
                registrationNumber = vehicle.RegistrationNumber,
                model = vehicle.Model,
                capacity = vehicle.Capacity,
                status = vehicle.Status,
                currentMileage = vehicle.CurrentMileage,
                fuelType = vehicle.FuelType,
                lastServiceDate = vehicle.LastServiceDate?.ToString("yyyy-MM-dd"),
                nextServiceDue = vehicle.NextServiceDue?.ToString("yyyy-MM-dd"),
                notes = vehicle.Notes,
                createdAt = vehicle.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            });
        }

        // ==================== USER MANAGEMENT ====================

        // GET: User Management Page
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Where(u => u.Role == "User")
                .OrderByDescending(u => u.RegisteredAt)
                .ToListAsync();
            return View("Users/Index", users);
        }

        // GET: User Details
        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _context.Users
                .Include(u => u.Bookings)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
                return NotFound();
            return View(user);
        }

        // POST: Deactivate User
        [HttpPost]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = user.IsActive });
        }

        // POST: Reset User Password (Admin initiated)
        [HttpPost]
        public async Task<IActionResult> ResetUserPassword(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            var resetToken = _userService.GenerateToken();
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.Now.AddHours(24);
            user.MustChangePassword = true;
            await _context.SaveChangesAsync();

            var resetLink = Url.Action("ResetPassword", "Account", new { email = user.Email, token = resetToken }, Request.Scheme);
            var placeholders = new Dictionary<string, string>
            {
                { "FullName", user.FullName },
                { "ResetLink", resetLink }
            };
            await _emailService.SendEmailWithTemplateAsync(user.Email, user.FullName, "PasswordReset", placeholders);

            return Json(new { success = true, message = "Password reset email sent" });
        }
        

        // GET: Get User Details (AJAX for modal popup) - NEW
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            var totalBookings = await _context.Bookings.CountAsync(b => b.UserId == userId);

            return Json(new
            {
                success = true,
                userId = user.UserId,
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                role = user.Role,
                isActive = user.IsActive,
                registeredAt = user.RegisteredAt.ToString("yyyy-MM-dd HH:mm"),
                lastLoginAt = user.LastLoginAt?.ToString("yyyy-MM-dd HH:mm"),
                totalBookings = totalBookings
            });
        }

        // POST: Toggle User Status (AJAX) - IMPROVED VERSION with better response
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus([FromBody] ToggleUserStatusRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();

                return Json(new { success = true, isActive = user.IsActive, message = $"User {(user.IsActive ? "activated" : "deactivated")} successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== BOOKING MANAGEMENT ====================

        // GET: All Bookings Page
        public async Task<IActionResult> Bookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(bookings);
        }

        // GET: Booking Details
        public async Task<IActionResult> BookingDetails(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Allocation)
                    .ThenInclude(a => a.Vehicle)
                .Include(b => b.Allocation)
                    .ThenInclude(a => a.Driver)
                        .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
                return NotFound();
            return View(booking);
        }

        // ==================== FUEL RECORDS ====================

        // GET: Fuel Records Page (with filtering)
        public async Task<IActionResult> FuelRecords(DateTime? fromDate, DateTime? toDate, int? vehicleId)
        {
            var query = _context.FuelRecords
                .Include(f => f.Vehicle)
                .Include(f => f.Driver)
                    .ThenInclude(d => d.User)
                //.Include(f => f.Issuer/*)*/
                .AsQueryable();

            // Apply date filters
            if (fromDate.HasValue)
                query = query.Where(f => f.DateIssued >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(f => f.DateIssued <= toDate.Value);

            // Apply vehicle filter
            if (vehicleId.HasValue && vehicleId.Value > 0)
                query = query.Where(f => f.VehicleId == vehicleId.Value);

            var fuelRecords = await query.OrderByDescending(f => f.DateIssued).ToListAsync();

            // Calculate totals for stats cards
            ViewBag.TotalLiters = fuelRecords.Sum(f => f.FuelLiters);
            ViewBag.TotalCost = fuelRecords.Sum(f => f.FuelCost);
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            // Get vehicles for filter dropdown
            ViewBag.Vehicles = await _context.Vehicles
                .Select(v => new { v.VehicleId, v.RegistrationNumber, v.Model })
                .ToListAsync();

            return View(fuelRecords);
        }

        // GET: Get Fuel Record Details (AJAX for modal)
        [HttpGet]
        public async Task<IActionResult> GetFuelRecord(int id)
        {
            var record = await _context.FuelRecords
                .Include(f => f.Vehicle)
                .Include(f => f.Driver)
                    .ThenInclude(d => d.User)
                //.Include(f => f.Issuer)
                .FirstOrDefaultAsync(f => f.FuelRecordId == id);

            if (record == null)
                return Json(new { success = false, message = "Record not found" });

            return Json(new
            {
                success = true,
                fuelRecordId = record.FuelRecordId,
                dateIssued = record.DateIssued.ToString("MMM dd, yyyy HH:mm"),
                vehicleReg = record.Vehicle?.RegistrationNumber ?? "N/A",
                vehicleModel = record.Vehicle?.Model ?? "N/A",
                driverName = record.Driver?.User?.FullName ?? "N/A",
                fuelLiters = record.FuelLiters,
                fuelCost = record.FuelCost,
                costPerLiter = record.CostPerLiter.ToString("N2"),
                currentMileage = record.CurrentMileage,
                receiptNumber = record.ReceiptNumber ?? "N/A",
                //issuedByName = record.Issuer?.FullName ?? "System",
                notes = record.Notes ?? "No notes"
            });
        }

        

        // ==================== TRIP REPORTS ====================

        // GET: Trip Reports Page (with filtering)
        public async Task<IActionResult> TripReports(DateTime? fromDate, DateTime? toDate, int? driverId, int? vehicleId)
        {
            var query = _context.TripReports
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Booking)
                        .ThenInclude(b => b.User)
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Vehicle)
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Driver)
                        .ThenInclude(d => d.User)
                .Where(t => t.ReportStatus == "Completed")
                .AsQueryable();

            // Apply date filters
            if (fromDate.HasValue)
                query = query.Where(t => t.EndTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.EndTime <= toDate.Value);

            // Apply driver filter
            if (driverId.HasValue && driverId.Value > 0)
                query = query.Where(t => t.Allocation.DriverId == driverId.Value);

            // Apply vehicle filter
            if (vehicleId.HasValue && vehicleId.Value > 0)
                query = query.Where(t => t.Allocation.VehicleId == vehicleId.Value);

            var tripReports = await query.OrderByDescending(t => t.EndTime).ToListAsync();

            // Calculate totals
            var totalDistance = tripReports.Sum(t => t.TotalDistance ?? 0);
            var totalFuel = tripReports.Sum(t => t.ActualFuelUsed ?? 0);
            var avgEfficiency = totalDistance > 0 ? (totalFuel / totalDistance * 100) : 0;

            ViewBag.TotalDistance = totalDistance;
            ViewBag.TotalFuel = totalFuel;
            ViewBag.AvgEfficiency = avgEfficiency;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            // Get drivers for filter dropdown
            ViewBag.Drivers = await _context.Drivers
                .Include(d => d.User)
                .Select(d => new { d.DriverId, Name = d.User.FullName })
                .ToListAsync();

            // Get vehicles for filter dropdown
            ViewBag.Vehicles = await _context.Vehicles
                .Select(v => new { v.VehicleId, v.RegistrationNumber, v.Model })
                .ToListAsync();

            return View(tripReports);
        }
        // GET: Get Trip Report Details (AJAX for modal)
        [HttpGet]
        public async Task<IActionResult> GetTripReport(int id)
        {
            var trip = await _context.TripReports
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Booking)
                        .ThenInclude(b => b.User)
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Vehicle)
                .Include(t => t.Allocation)
                    .ThenInclude(a => a.Driver)
                        .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(t => t.TripReportId == id);

            if (trip == null)
                return Json(new { success = false, message = "Trip report not found" });

            var distance = trip.TotalDistance ?? 0;
            var fuelUsed = trip.ActualFuelUsed ?? 0;
            var efficiency = distance > 0 ? (fuelUsed / distance * 100) : 0;

            return Json(new
            {
                success = true,
                tripReportId = trip.TripReportId,
                bookingId = trip.Allocation?.BookingId,
                endTime = trip.EndTime?.ToString("MMM dd, yyyy 'at' hh:mm tt"),
                driverName = trip.Allocation?.Driver?.User?.FullName,
                vehicleReg = trip.Allocation?.Vehicle?.RegistrationNumber,
                vehicleModel = trip.Allocation?.Vehicle?.Model,
                destination = trip.Allocation?.Booking?.Destination,
                passengerName = trip.Allocation?.Booking?.User?.FullName,
                passengerPhone = trip.Allocation?.Booking?.User?.PhoneNumber,
                startMileage = trip.StartMileage,
                endMileage = trip.EndMileage,
                totalDistance = distance,
                fuelUsed = fuelUsed,
                efficiency = efficiency.ToString("N1"),
                driverNotes = trip.DriverNotes,
                adminNotes = trip.AdminNotes
            });
        }

        // ==================== ALLOCATION (AJAX) ====================

        // GET: Available Vehicles (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetAvailableVehicles()
        {
            var vehicles = await _context.Vehicles
                .Where(v => v.Status == "Available")
                .Select(v => new { v.VehicleId, v.Model, v.RegistrationNumber })
                .ToListAsync();
            return Json(vehicles);
        }

        // GET: Available Drivers (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetAvailableDrivers()
        {
            var drivers = await _context.Drivers
                .Include(d => d.User)
                .Where(d => d.IsAvailable)
                .Select(d => new { d.DriverId, Name = d.User.FullName, d.LicenseNumber })
                .ToListAsync();
            return Json(drivers);
        }

        // POST: Allocate Booking
        [HttpPost]
        public async Task<IActionResult> AllocateBooking([FromBody] AllocationRequest request)
        {
            try
            {
                var booking = await _context.Bookings.Include(b => b.User).FirstOrDefaultAsync(b => b.BookingId == request.BookingId);
                if (booking == null || booking.Status != "Pending")
                    return Json(new { success = false, message = "Booking not found or already processed" });

                var vehicle = await _context.Vehicles.FindAsync(request.VehicleId);
                var driver = await _context.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.DriverId == request.DriverId);

                if (vehicle == null || driver == null)
                    return Json(new { success = false, message = "Invalid vehicle or driver" });

                var allocation = new Allocation
                {
                    BookingId = request.BookingId,
                    VehicleId = request.VehicleId,
                    DriverId = request.DriverId,
                    ApprovedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                    ApprovalDate = DateTime.Now,
                    AllocationStatus = "Assigned",
                    NotesForDriver = request.NotesForDriver,
                    NotesForPassenger = request.NotesForPassenger
                };

                _context.Allocations.Add(allocation);
                booking.Status = "Approved";
                vehicle.Status = "InUse";
                driver.IsAvailable = false;
                await _context.SaveChangesAsync();

                // Send email to Passenger
                var passengerTrackingLink = Url.Action("MyBookings", "User", null, Request.Scheme);
                var passengerPlaceholders = new Dictionary<string, string>
                {
                    { "PassengerName", booking.User.FullName },
                    { "DriverName", driver.User.FullName },
                    { "DriverPhone", driver.PhoneNumber ?? "Not provided" },
                    { "VehicleReg", vehicle.RegistrationNumber },
                    { "PickupDate", booking.StartDate.ToString("MMM dd, yyyy 'at' hh:mm tt") },
                    { "TrackingLink", passengerTrackingLink }
                };
                await _emailService.SendEmailWithTemplateAsync(booking.User.Email, booking.User.FullName, "BookingConfirmation", passengerPlaceholders);

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

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Reject Booking
        [HttpPost]
        public async Task<IActionResult> RejectBooking([FromBody] RejectRequest request)
        {
            var booking = await _context.Bookings.FindAsync(request.BookingId);
            if (booking == null || booking.Status != "Pending")
                return Json(new { success = false, message = "Booking not found" });

            booking.Status = "Rejected";
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

    // ==================== REQUEST CLASSES ====================

    public class AllocationRequest
    {
        public int BookingId { get; set; }
        public int VehicleId { get; set; }
        public int DriverId { get; set; }
        public string NotesForDriver { get; set; }
        public string NotesForPassenger { get; set; }
    }

    public class RejectRequest
    {
        public int BookingId { get; set; }
    }

    public class CreateDriverRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public DateTime? HireDate { get; set; }
        public bool IsAvailable { get; set; }
        public string Notes { get; set; }
    }

    public class UpdateDriverRequest
    {
        public int DriverId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public DateTime? HireDate { get; set; }
        public bool IsAvailable { get; set; }
        public string Notes { get; set; }
    }

    public class DeleteDriverRequest
    {
        public int DriverId { get; set; }
    }

    public class SetDriverPasswordRequest
    {
        public int DriverId { get; set; }
        public string NewPassword { get; set; }
    }
    public class ToggleUserStatusRequest
    {
        public int UserId { get; set; }
    }
}