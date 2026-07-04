# MediCare – Clinic Management System

A full-stack clinic management system built with **ASP.NET Core MVC (.NET 8)** for managing doctors, patients, specialties, and appointments. The application provides secure role-based access using ASP.NET Core Identity and follows the Code-First approach with Entity Framework Core.

---

## Tech Stack

- ASP.NET Core MVC (.NET 8)
- C#
- Entity Framework Core (Code First)
- SQL Server
- ASP.NET Core Identity
- Bootstrap 5
---
On the first run, **DbSeeder** automatically:

- Creates the required Identity roles
- Creates the default administrator account
- Seeds specialties
- Seeds sample doctor accounts


#  Features

## Admin

- Dashboard with live system statistics
- Manage doctors (Create, Edit, Delete)
- Upload doctor profile images
- Automatically create login accounts for doctors
- Manage medical specialties
- View all registered patients
- View and filter appointments by status

---

## Doctor

- View assigned appointments
- Access patient information
- Review appointment history
- Update appointment status
- Add consultation notes

---

## Patient

- Register, Login, and Logout
- Update personal profile
- Search doctors by name or specialty
- Book appointments
- View appointment history
- Cancel pending or confirmed appointments

---

## Security

- ASP.NET Core Identity Authentication
- Role-Based Authorization (Admin / Doctor / Patient)
- Ownership checks to prevent unauthorized data access
- Server-side and Client-side validation using Data Annotations


## Highlights

- ASP.NET Core MVC (.NET 8)
- Entity Framework Core (Code First)
- SQL Server
- ASP.NET Core Identity
- Role-Based Authorization
- CRUD Operations
- File Upload
- ViewModels
- Data Validation
- Responsive Bootstrap 5 UI
