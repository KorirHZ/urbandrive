using Microsoft.EntityFrameworkCore;
using UrbanDrive.Data;
using UrbanDrive.Models;
using UrbanDrive.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 🔴 REGISTER DATABASE CONTEXT (This was missing)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Register MailSettings from appsettings.json
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("SmtpSettings"));

// 🔴 REGISTER CUSTOM SERVICES (This was missing)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// 🔴 ADD SESSION SUPPORT (This was missing)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 🔴 ADD AUTHENTICATION (This was missing)
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "UrbanDriveAuth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🔴 ORDER MATTERS - These must be in this sequence
app.UseSession();           // Session before authentication
app.UseAuthentication();    // Authentication before authorization
app.UseAuthorization();     // Authorization last

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
//// Seed Admin Account
//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

//    // Check if admin exists
//    if (!await context.Users.AnyAsync(u => u.Role == "Admin"))
//    {
//        var adminUser = new User
//        {
//            FullName = "System Administrator",
//            Email = "admin@urbandrive.com",
//            PhoneNumber = "0712345678",
//            Role = "Admin",
//            IsActive = true,
//            IsEmailVerified = true,
//            RegisteredAt = DateTime.Now,
//            MustChangePassword = false
//        };

//        // Use your password HOOD@123
//        await userService.RegisterUserAsync(adminUser, "HOOD@123");
//        Console.WriteLine("✅ Admin account created: admin@urbandrive.com / HOOD@123");
//    }
//}

app.Run();