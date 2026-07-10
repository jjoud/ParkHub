using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using ParkHub.Data;
using ParkHub.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw(
        "IF COL_LENGTH('Users', 'Role') IS NULL ALTER TABLE Users ADD Role nvarchar(20) NOT NULL CONSTRAINT DF_Users_Role DEFAULT N'Customer';");

    // Development-only seed data: add test user, vehicle and parking spaces if missing
    if (app.Environment.IsDevelopment())
    {
        if (!db.Users.Any(u => u.Email.ToLower() == "admin@parkhub.com"))
        {
            var adminUser = new ParkHub.Models.User
            {
                FullName = "Admin User",
                Email = "admin@parkhub.com",
                PhoneNumber = "0500000001",
                Role = "Admin"
            };
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "admin123");
            db.Users.Add(adminUser);
            db.SaveChanges();
        }

        if (!db.Users.Any(u => u.Email.ToLower() == "test@example.com"))
        {
            var user = new ParkHub.Models.User
            {
                FullName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "0500000000",
                Role = "User"
            };
            user.PasswordHash = passwordHasher.HashPassword(user, "seeded");
            db.Users.Add(user);
            db.SaveChanges();

            db.Vehicles.Add(new ParkHub.Models.Vehicle
            {
                PlateNumber = "TEST123",
                VehicleType = "Sedan",
                Color = "Blue",
                UserId = user.UserId
            });
            db.SaveChanges();
        }

        var areas = new[] { "Area A", "Area B", "Area C", "Area D" };
        foreach (var area in areas)
        {
            for (int i = 1; i <= 12; i++)
            {
                var spaceNumber = i.ToString("00");
                if (!db.ParkingSpaces.Any(p => p.AreaName == area && p.SpaceNumber == spaceNumber))
                {
                    db.ParkingSpaces.Add(new ParkHub.Models.ParkingSpace
                    {
                        AreaName = area,
                        SpaceNumber = spaceNumber,
                        SpaceType = "Standard",
                        Status = false
                    });
                }
            }
        }

        db.SaveChanges();
    }

    var usersWithPlainTextPasswords = db.Users
        .Where(user => !user.PasswordHash.StartsWith("AQAAAA"))
        .ToList();

    foreach (var user in usersWithPlainTextPasswords)
    {
        user.PasswordHash = passwordHasher.HashPassword(user, user.PasswordHash);
    }

    if (usersWithPlainTextPasswords.Any())
    {
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
