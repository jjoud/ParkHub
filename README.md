# ParkHub 🚘

Smart Parking Management System built with **ASP.NET Core MVC** and **SQL Server**.

## Project Features

* View available and reserved parking spaces.
* Add and manage customer vehicles.
* Create parking reservations.
* Prevent reserving an already reserved parking space.
* Calculate parking fees based on reservation hours.
* Track reservation and payment status.
* Dashboard showing occupancy rate, reservations, and revenue.

## Important Rules

* Do not work directly on the `main` branch.
* Every member must create a separate branch before starting work.
* Pull the latest changes before starting any task.
* Do not change another member’s files without informing the team.
* Use clear commit messages.
* After finishing a task, create a Pull Request to merge changes into `main`.

---

## First Time Setup

Clone the project:

```bash
git clone REPOSITORY_LINK
```

Open the project folder:

```bash
cd ParkHub
```

Open the solution file:

```bash
ParkHub.sln
```

---

## How to Start Working

First, get the latest project changes:

```bash
git checkout main
git pull origin main
```

Create your own branch:

```bash
git checkout -b feature/your-task-name
```

Examples:

```bash
git checkout -b feature/parking-management
git checkout -b feature/vehicle-management
git checkout -b feature/reservations
git checkout -b feature/dashboard
```

---

## How to Upload Your Work

Check changed files:

```bash
git status
```

Add your files:

```bash
git add .
```

Save your changes with a message:

```bash
git commit -m "Add parking management CRUD"
```

Upload your branch:

```bash
git push origin feature/your-task-name
```

After uploading, open GitHub and create a **Pull Request** from your branch into `main`.

---

## Before Creating a Pull Request

Always pull the latest `main` branch first:

```bash
git checkout main
git pull origin main
git checkout feature/your-task-name
git merge main
```

Fix any conflicts if they appear, then push again:

```bash
git push origin feature/your-task-name
```

---

## Branch Names

Use this format:

```text
feature/parking-management
feature/vehicle-management
feature/account-authentication
feature/reservation-logic
feature/dashboard-ui
feature/database-models
```

---

## Main Branch

The `main` branch is the final stable version of the project.

Do not push directly to `main`.
