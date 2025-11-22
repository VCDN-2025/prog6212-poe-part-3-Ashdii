# CMCSystem – Part 3

## Overview
**CMCSystem** (Claim Management System) is a web-based application built using **ASP.NET Core MVC**. It allows **lecturers** to submit hourly claims for work performed, track claim status, and manage supporting documents. **HR administrators** can review, approve, and manage all submitted claims.  

This is **Part 3** of the project, focusing on:
- Lecturer claim submission and editing  
- Dashboard and claim history  
- File uploads for supporting documents  
- HR claim management and approvals  

---

## Features

### Lecturer Features
- Dashboard displaying current hourly rate.  
- Submit new claims with:  
  - Hours worked  
  - Automatic calculation of total claim amount  
  - Optional supporting document upload (PDF, Word, Excel, images).  
- View submitted claims and their status.  
- Edit pending claims.  
- Download uploaded supporting documents.

### HR Features
- View all submitted claims.  
- Access lecturer information linked to claims.  
- Download supporting documents for verification.

### General
- Session-based authentication and role management (Lecturer / HR).  
- Input validation and file upload restrictions (max 5MB, allowed file types).  
- Error handling and notifications via TempData.  

---

## Technologies Used
- **ASP.NET Core MVC 9.0**  
- **Entity Framework Core 9.0.3** (Database interactions)  
- **SQL Server LocalDB**  
- **Bootstrap 5** (Frontend styling)  
- **C#**  

---

## Database Structure
- **Lecturers**: Contains lecturer details (FullName, Email, HourlyRate, PhoneNumber, etc.)  
- **Claims**: Tracks claims submitted by lecturers, including:  
  - LecturerId (foreign key)  
  - HoursWorked  
  - HourlyRate  
  - TotalAmount  
  - Status (Pending, Approved, Rejected)  
  - SupportingDocumentPath  
  - DateSubmitted  
- **Users**: Handles authentication (Email, Password, Role, IsActive)  

---

## Setup Instructions

1. **Clone the repository**
```bash
git clone <repository-url>
cd CMCSystem
```

2. **Restore NuGet packages**
```bash
dotnet restore
```

3. **Update Database**
- Ensure `ApplicationDbContext` connection string in `appsettings.json` points to your SQL Server LocalDB.
- Apply migrations:
```bash
dotnet ef database update
```

4. **Run the application**
```bash
dotnet run
```
- Access via `https://localhost:5001` (or the port displayed in console).

---

## Usage Instructions

### Lecturer
1. Login using a lecturer account.  
2. Access the **Dashboard** to view current hourly rate.  
3. Click **Submit Claim** to create a new claim.  
4. View all claims under **My Claims**.  
5. Edit pending claims if needed.  
6. Download any supporting document.

### HR
1. Login using an HR account.  
2. Access **Claims** to view all submitted claims.  
3. Download supporting documents for verification.  

---

## Notes
- Uploaded files are stored in `wwwroot/uploads`.  
- Session-based role verification ensures restricted access to lecturers and HR functionalities.  
- Total claim amounts are automatically calculated from `HoursWorked × HourlyRate`.  
- Claims can only be edited while status is **Pending**.  

---

## Future Enhancements
- Add HR claim approval/rejection workflow with comments.  
- Email notifications to lecturers on claim status changes.  
- Enhanced analytics dashboard for HR (total claims, approved/rejected).  
- Role-based access for department managers.  

---

## Author
**Sandiswa Memela**  
Student – CMCSystem Project Part 3

