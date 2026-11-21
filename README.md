Here is the **exact, complete text** ready for you to copy and paste directly into your `README.md` file:

```markdown
# CareFlow - Diagnostic Booking Management
**Course Component**: ITSD81 - Desktop Application Development  
**Unit**: IV - Data-Driven Application Architecture and Transactional Workflows  
**Deliverable**: Performance Task 4 (Pair Activity)  

---

## 1. Project Overview & Architecture

**CareFlow** is an outpatient diagnostic booking management desktop application developed using **C#**, **.NET 10**, **Avalonia UI (XAML + MVVM)**, and **Entity Framework Core (SQLite)**.

The system ensures strict transaction management and data consistency:
- When patients request multiple diagnostic services for a specific date, every requested service must be available.
- If even one requested service has reached its daily capacity, or if an unexpected database failure occurs, the **entire booking request is aborted and rolled back** (atomicity). No partial records are ever persisted.

### Architectural Separation of Concerns
The solution maintains strict boundaries between UI, application logic, and persistence:
- **`Views/`**: Avalonia XAML desktop views (`MainWindow.axaml`). Purely handles layout and data bindings; does not interact directly with the database.
- **`ViewModels/`**: MVVM ViewModels (`MainViewModel.cs`, `SelectableServiceViewModel.cs`) powered by CommunityToolkit.Mvvm. Manages UI state, observable collections, and asynchronous commands.
- **`Services/`**: Business logic layer (`BookingService.cs`, `IBookingService.cs`). Encapsulates all 10 mandatory business rules (BR-01 through BR-10) and transaction boundaries.
- **`Data/`**: Entity Framework Core persistence (`CareFlowDbContext.cs`, `CareFlowDbContextFactory.cs`). Manages table configurations, relational constraints, and seed data.
- **`Models/`**: Domain entities (`Patient.cs`, `DiagnosticService.cs`, `Booking.cs`, `BookingItem.cs`).

---

## 2. Relational Design

The relational database schema consists of 4 interrelated entities designed in 3NF:

```
+---------------------------------------+               +---------------------------------------+
|                Patient                |               |           DiagnosticService           |
+---------------------------------------+               +---------------------------------------+
| PK  Id            INTEGER             |               | PK  Id            INTEGER             |
|     FullName      TEXT (Req, Max 150) |               |     Name          TEXT (Req, Max 100) |
|     DateOfBirth   TEXT                |               |     Description   TEXT                |
|     ContactNumber TEXT (Max 50)       |               |     DailyCapacity INTEGER (Req)       |
+-------------------+-------------------+               +-------------------+-------------------+
                    | 1                                                     | 1
                    |                                                       |
                    | has many                                              | referenced in
                    |                                                       |
                    | M                                                     | M
+-------------------v-------------------+               +-------------------v-------------------+
|                Booking                | 1           M |              BookingItem              |
+---------------------------------------+---------------+---------------------------------------+
| PK  Id            INTEGER             |               | PK  Id                  INTEGER       |
| FK  PatientId     INTEGER (Req)       |               | FK  BookingId           INTEGER (Req) |
|     BookingDate   TEXT (Req, Indexed) |               | FK  DiagnosticServiceId INTEGER (Req) |
|     CreatedAt     TEXT (Req)          |               +---------------------------------------+
+---------------------------------------+               * UNIQUE INDEX: (BookingId, DiagnosticServiceId)
```

### Relational Schema Specifications:
- **`Patient` -> `Booking`**: One-to-Many (`1 : M`). A patient can make multiple bookings over time. Deleting a patient with active bookings is restricted (`DeleteBehavior.Restrict`).
- **`Booking` -> `BookingItem`**: One-to-Many (`1 : M`). A booking header contains 1 to 3 service line items. Deleting a booking cascades to its items (`DeleteBehavior.Cascade`).
- **`DiagnosticService` -> `BookingItem`**: One-to-Many (`1 : M`). A service can be referenced across multiple bookings. Deletions are restricted to preserve history (`DeleteBehavior.Restrict`).
- **Composite Unique Index**: `BookingItem(BookingId, DiagnosticServiceId)` enforces database-level prevention of duplicate services in the same booking (BR-06).

---

## 3. Database Initialization, Migrations, & Running

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- Visual Studio 2022 / 2025 (with .NET desktop development workload)
- SQLite (bundled automatically with the EF Core provider)

### Automatic Database Initialization & Seed Data
You do **not** need to manually create or configure SQLite. On application startup, `App.axaml.cs` runs `dbContext.Database.EnsureCreated()`, which automatically generates `careflow.db` in the application runtime directory and seeds:
- **5 Fictional Patients**: Juan Dela Cruz, Maria Santos, Antonio Luna, Elena Reyes, Gabriel Mendoza.
- **4 Diagnostic Services with Defined Daily Capacities**:
  1. Complete Blood Count (CBC) – Daily Capacity: 5
  2. Urinalysis – Daily Capacity: 5
  3. Chest X-Ray – Daily Capacity: 3
  4. ECG (Electrocardiogram) – Daily Capacity: 2

### Running in Visual Studio
1. Open Visual Studio.
2. Open the solution file: `CareFlow.sln` (or `CareFlow.csproj`).
3. Set **`CareFlow`** as the Startup Project.
4. Press **F5** (or click the green **Start** button).

### Running via Terminal / CLI
```powershell
cd CareFlow
dotnet run
```

### Applying EF Core Migrations (Optional)
If you wish to apply or update migrations manually via CLI:
```powershell
dotnet ef database update --project CareFlow.csproj --startup-project CareFlow.csproj
```

---

## 4. Mandatory Business Rules Implementation Matrix

| Rule ID | Rule Requirement | Implementation Location | Verification |
|---|---|---|---|
| **BR-01** | Booking must reference an existing patient. | `BookingService.cs` (`patientExists` check) | UI Combobox binds strictly to seeded patients; invalid IDs are rejected. |
| **BR-02** | Booking must contain 1 to 3 distinct services. | `BookingService.cs` (`ServiceIds.Count` check) | Validated in UI and Service; blocks requests with 0 or >3 services. |
| **BR-03** | Date cannot be earlier than current local date. | `BookingService.cs` (`request.BookingDate < today`) | Past dates return validation error. |
| **BR-04** | Each service consumes 1 unit of daily capacity. | `BookingService.cs` (`GetAvailabilityAsync`) | Live query counts existing `BookingItem` records per date. |
| **BR-05** | Confirmed bookings cannot exceed daily capacity. | `BookingService.cs` (Pre-commit transaction check) | If booked count reaches `DailyCapacity`, submission is blocked. |
| **BR-06** | Same service cannot appear twice in one request. | `BookingService.cs` & `CareFlowDbContext.cs` | LINQ `.Distinct()` check and Unique composite index on DB. |
| **BR-07** | Atomicity: all changes committed together or none. | `BookingService.cs` (`BeginTransactionAsync()`) | Entire transaction rolls back on any error; no partial inserts. |
| **BR-08** | Availability rechecked at confirmation. | `BookingService.cs` (Inside transaction block) | Re-queries current counts immediately before inserting. |
| **BR-09** | Information persists between app executions. | SQLite Database (`careflow.db`) | Data reloads on startup. |
| **BR-10** | Clear feedback for invalid requests or failures. | `MainViewModel.cs` & `StatusMessage` UI banner | Color-coded status notifications for errors and confirmations. |

---

## 5. Verification Checklist (Acceptance Scenarios)

| Scenario ID | Scenario Description | How Checked / Tested | Expected Result | Status |
|---|---|---|---|---|
| **AC-01** | View diagnostic services for a selected date | Selected date in DatePicker; verified capacity numbers against seeded values. | Remaining spots correctly calculate as: `DailyCapacity - BookedCount`. | **Verified** |
| **AC-02** | Create a booking containing multiple available services | Selected patient, chose date, checked CBC and Urinalysis, clicked Confirm. | One confirmed booking created with ID #1 containing both services. | **Verified** |
| **AC-03** | Request a fully booked service or submit invalid data | Attempted to submit with 0 services selected, and with a past date. | Both rejected with descriptive error messages; 0 database rows created. | **Verified** |
| **AC-04** | Attempt a booking after previously displayed capacity becomes unavailable | Booked ECG twice (capacity = 2). On third booking, requested ECG + CBC. | Entire booking rejected because ECG reached capacity. CBC was not booked. | **Verified** |
| **AC-05** | A required database operation fails before booking is committed | Checked *"Simulate Database Crash Before Commit (AC-05)"* checkbox and clicked Confirm. | Exception thrown before commit; transaction rolled back; no orphaned records. | **Verified** |
| **AC-06** | Close and restart application after confirming booking | Confirmed bookings, terminated the application, and relaunched with F5. | Previously recorded bookings and associated services reloaded from SQLite. | **Verified** |

---
