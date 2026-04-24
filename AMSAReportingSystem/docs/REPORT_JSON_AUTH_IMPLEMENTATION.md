# AMSA Reporting System: JSON Report + Auth/Access Implementation

## Summary
This document describes the implementation completed for:
- JSON-based department report storage (instead of one table per department).
- AMSA API-aligned authentication wiring.
- Role-based authorization for report editing and submission.
- End-to-end Blazor page wiring for authenticated report editing.

The approach is intentionally simple:
- small services
- direct method calls
- no heavy architecture layers

---

## 1. Data Model and Migration Changes

### 1.1 Department report model changed to JSON payload
`DepartmentReport` now stores form payload in:
- `ReportData` (`nvarchar(max)` JSON string)

Removed old navigation/table pattern for:
- `TaleemReport`
- `TablighReport`
- `WelfareReport`
- `SportReport`
- `FinanceReport`
- `HealthReport`
- `SecondarySchoolReport`
- `TajneedReport`
- `GeneralReport`

### 1.2 Files updated
- `Data/Entities/CoreEntities.cs`
- `Data/AmsaReportingDbContext.cs`
- `Data/Entities/DepartmentReports.cs` (deleted)
- `AMSAReportingSystem.csproj` (EF tools package added)

### 1.3 Seed stability fix for EF migrations
To stop EF "pending model changes" on every run:
- replaced dynamic `DateTime.UtcNow` seed values with deterministic fixed timestamp in `SeedData`.

### 1.4 Migration status
Migration files were generated in:
- `Migrations/`

Database update now works in your environment after tool/runtime updates.

---

## 2. AMSA API Auth Contract Alignment

Based on AMSA auth endpoint behavior in:
- `C:/Users/DELL/Desktop/Coded/AmsaAPI/FastEndpoints/AuthEndpoints.cs`

`/api/auth/token` can return:
- `token`
- `tokenType`

`expiresIn` may be missing, so client handling was adjusted:
- `TokenResponse.ExpiresIn` is nullable.
- `AmSaAuthService` falls back to 3600 seconds when missing.

Files updated:
- `Services/IAmSaApiClient.cs`
- `Services/AmSaAuthService.cs`

---

## 3. Authorization Rules Implemented

Rules implemented exactly around your requested scopes:

### 3.1 Department officers
- Can edit only their own department.
- Unit-level scope: own unit only.
- State-level office holder for department: same department across units in own state.
- National-level office holder for department: same department across all states.

### 3.2 Unit leadership
- Unit President / Unit GS can edit all departments in their unit.

### 3.3 State leadership
- State President / State GS can edit all departments for units in their state.

### 3.4 National leadership
- National President / National GS / Assistant GS can edit all departments across all units.

### 3.5 Submission authorization
Report submission uses the same scope intent:
- unit actors for own unit
- state actors for own state
- national actors globally

Core authorization logic file:
- `Services/ReportAccessService.cs`

---

## 4. Service Layer Added

## 4.1 ReportService
File:
- `Services/ReportService.cs`

Responsibilities:
- Get/create draft report.
- Ensure unit exists locally (sync from AMSA API when needed).
- Save department JSON payload.
- Mark department submitted.
- Submit full report to president.
- Write report activity logs.
- Enforce authorization checks through `ReportAccessService`.

## 4.2 CurrentUserReportService
File:
- `Services/CurrentUserReportService.cs`

Purpose:
- Simple adapter from current authenticated Blazor user to `ReportService`.
- Avoids repeating user lookup on every page action.

Methods:
- `GetOrCreateCurrentUserDraftAsync`
- `GetDepartmentAsync`
- `SaveDepartmentJsonAsync`
- `SubmitReportToPresidentAsync`
- `CanEditOwnUnitDepartment`

---

## 5. Blazor Wiring

### 5.1 New department editor page
File:
- `Components/Pages/DepartmentReportEditor.razor`

Route:
- `/report/{DepartmentSlug}`

Features:
- Loads/creates current user draft report.
- Loads JSON payload for selected department.
- Save draft JSON.
- Submit department.
- Submit full report to president.

### 5.2 Hard route blocking (requested)
Unauthorized users are blocked from department routes:
- if user cannot edit that department in scope, page redirects to `/dashboard`.

This is done early in `OnInitializedAsync` using:
- `CurrentUserReportService.CanEditOwnUnitDepartment(...)`

### 5.3 Visibility blocking (requested)
Unauthorized report links/buttons are hidden (not only blocked on click):

- `DepartmentOfficerDashboard` now renders department cards dynamically from allowed permissions only.
- `NavMenu` now shows `My Report` only when user has at least one accessible department.
- `My Report` route is generated from first allowed department instead of hardcoded `/report/taleem`.

### 5.4 Dashboard role-priority fix (MVP stability)
Users with multiple roles were previously routed to officer dashboard first.
Now dashboard chooses higher-level views first:
1. National
2. State
3. Unit
4. Department officer

This avoids incorrect dashboard rendering for users holding multiple positions.

---

## 6. Dependency Injection Changes

File:
- `Program.cs`

Registered:
- `AmsaAuthStateProvider` as concrete service
- `AuthenticationStateProvider` mapped to same concrete instance
- `ReportAccessService`
- `ReportService`
- `CurrentUserReportService`

This allows both Blazor auth state and business services to share the same current user context cleanly.

---

## 7. Build/Verification

After these changes:
- project builds successfully (`0 errors`, `0 warnings`).

---

## 8. Notes and Current Constraints

1. Department editor currently uses a fixed cycle id (`1`) for simplicity.
2. JSON editor is raw payload mode (textarea), by design for fast delivery and flexibility.
3. Access control is fully enforced at service level (not UI-only), so direct page calls still require permission.
4. AMSA role matching currently uses string patterns like:
   - `"Department:DepartmentOfficer"`
   - `"President:UnitPresident"`
   - `"GeneralSecretary:StateGS"`
   adapt names if AMSA backend role naming changes.

---

## 9. Suggested Next Steps

1. Add a compact JSON schema helper per department for guided form generation.
2. Add unit tests for `ReportAccessService` role matrix.
3. Add export/report download support (PDF/CSV) for state and national summaries.
4. Add persistence for auth session so users remain logged in across reloads.

---

## 10. MVP Realism Pass Completed

The following were implemented to move from placeholder UI to working MVP behavior:

1. Active cycle now comes from DB (`GetOrCreateActiveCycleAsync`) instead of hardcoded cycle id.
2. Unit President dashboard now uses real reports and supports:
   - approve + forward to state
   - reject at unit level
3. State GS/President dashboard now uses real state reports and supports:
   - approve + forward to national
   - reject at state level
4. National dashboard now uses real national queue and supports:
   - acknowledge reports submitted to national
5. Department officer dashboard now shows:
   - live cycle label
   - live submission deadline
   - live submitted/total progress from database
6. Dashboard routing now prioritizes higher-level roles first (national/state/unit/officer).
7. Unauthorized department links are both:
   - hidden in UI/nav
   - blocked at route/service level
