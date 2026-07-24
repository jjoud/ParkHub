# ParkHub

ParkHub is a smart parking management system built with ASP.NET Core MVC and SQL Server. The project helps customers find available parking spaces, register their vehicles, create reservations, and complete simple payments. It also provides an admin experience for managing parking spaces and viewing system statistics.

This project was created after completing an ASP.NET Core course at Tuwaiq Academy.

[![View Case Study](https://img.shields.io/badge/View-Case%20Study-2ea44f?style=for-the-badge)](https://verdant-scone-e58f1a.netlify.app/)

## Project Idea

The main idea of ParkHub is to make parking management easier for both customers and administrators.

Customers can:

- Create an account and sign in.
- View parking areas and parking space availability.
- Register and manage their vehicles.
- Reserve available parking spaces.
- View reservation history.
- Complete payment for reservations.

Admins can:

- Sign in with an admin account.
- View the dashboard.
- Add, edit, view, and manage parking spaces.
- See available and reserved spaces.
- Manage the status of parking spaces.
- Review general system statistics such as parking count, reservations, users, revenue, and occupancy rate.

## User Roles

The system supports two main role types:

- Admin: Can access admin features such as dashboard and parking management.
- Customer: Can access customer features such as vehicles, reservations, and parking browsing.

Sample development accounts:

```text
Admin
Email: admin@parkhub.com
Password: admin123

Customer
Email: test@example.com
Password: seeded
```

## Main Features

- Account registration and login.
- Role-based navigation and page access.
- Parking space listing, details, creation, editing, deletion, filtering, and search.
- Parking status filters for available, reserved, and VIP spaces.
- Vehicle CRUD operations for logged-in users.
- Reservation creation, editing, deletion, details, and history.
- Reservation ownership protection so users cannot manage other users' reservations.
- Automatic parking status updates when reservations are created or removed.
- Simple payment flow with duplicate payment prevention.
- Admin dashboard with basic statistics.
- SQL Server database integration using Entity Framework Core.

## Security

ParkHub includes several security features:

- Cookie-based authentication.
- Role-based authorization for admin-only pages.
- Password hashing using ASP.NET Core Identity `PasswordHasher`.
- Reservation ownership checks to protect user data.
- Admin-only access for parking management actions.
- Duplicate payment prevention.
- Anti-forgery validation on important form submissions.
- Automatic conversion of old plain-text development passwords into hashed passwords when the application starts.

## Technologies Used

- C#
- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- Razor Views
- Bootstrap
- jQuery
- jQuery Validation
- Cookie Authentication
- ASP.NET Core Identity PasswordHasher
- LINQ
- HTML
- CSS
- JavaScript

## Project Structure

```text
Controllers/
  AccountController.cs
  HomeController.cs
  ParkingController.cs
  ReservationController.cs
  VehicleController.cs

Models/
  User.cs
  Vehicle.cs
  ParkingSpace.cs
  Reservation.cs
  Payment.cs
  ViewModels/

Data/
  ApplicationDbContext.cs

Views/
  Account/
  Home/
  Parking/
  Reservation/
  Vehicle/
  Shared/

wwwroot/
  css/
  js/
  lib/

Migrations/
```

## Database

The system uses SQL Server with Entity Framework Core. Main tables include:

- Users
- Vehicles
- ParkingSpaces
- Reservations
- Payments

The application connection string is configured in `appsettings.json`.

## How To Run

1. Open the project in Visual Studio.
2. Make sure SQL Server is running.
3. Check the connection string in `appsettings.json`.
4. Build the project.
5. Run the application.
6. Sign in using the sample admin or customer account.

## Notes

This project was developed after an ASP.NET Core course at Tuwaiq Academy. It is designed as a university smart parking management system and focuses on demonstrating MVC structure, database relationships, authentication, role-based access, parking management, vehicle management, reservations, payments, and dashboard reporting.
