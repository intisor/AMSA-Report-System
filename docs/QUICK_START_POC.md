# Quick Start - Frontend POC Testing

## Prerequisites
- .NET 10 SDK installed
- Visual Studio 2025 or Visual Studio Code with C# extension
- Workspace: `C:\Users\DELL\Desktop\Coded\AMSA\AMSAReportingSystem\`

---

## Run the Application

### Option 1: From Visual Studio (Recommended)
1. Open `AMSAReportingSystem.sln`
2. Set `AMSAReportingSystem` as startup project
3. Press **F5** or click **Run** button
4. App starts at `https://localhost:7001/` (or configured HTTPS port)
5. Login page loads automatically

### Option 2: From Command Line
```powershell
cd C:\Users\DELL\Desktop\Coded\AMSA\AMSAReportingSystem
dotnet run --project AMSAReportingSystem\AMSAReportingSystem.csproj
```
Then navigate to `https://localhost:5001/` (or displayed port)

### Option 3: With Hot Reload (Development)
```powershell
dotnet watch --project AMSAReportingSystem\AMSAReportingSystem.csproj
```
Changes to .razor files auto-reload in browser

---

## Test Login Credentials

All test users use **any password** (mocked auth doesn't validate password)

| MKAN ID | Role | Dashboard | What You'll See |
|---------|------|-----------|-----------------|
| **10001** | Department Officer | DepartmentOfficerDashboard | Single section (Taleem), Progress 1/9, Notifications |
| **10002** | Unit President | UnitPresidentDashboard | All 9 departments, Compliance badges, Timeline, Approval form |
| **20001** | State General Secretary | StateGSDashboard | Stat cards, Unit submission table, Compliance % bars |
| **30001** | National General Secretary | NationalDashboard | System stats (450 units), Escalations, Top states |
| **30002** | National President | NationalDashboard | Same as National GS (both have national access) |

---

## Step-by-Step Workflow Tests

### Test 1: Officer Submits Form
1. Login with **10001**
2. Land on DepartmentOfficerDashboard
3. Click **"Edit Form →"** on Taleem section
4. Redirected to `/report/taleem` form
5. Fill in fields:
   - **Sessions Organized:** 5
   - **Total Members:** 40
   - **Average Attendance:** 35
6. Watch badges update:
   - ✓ Sessions ≥ 4? → YES
   - ✓ Attendance ≥ 75%? → YES (35/40 = 87.5%)
   - Overall: ✓ **COMPLIANT**
7. See "Saving..." briefly, then "Saved at HH:mm:ss"
8. Click back arrow → return to officer dashboard

### Test 2: President Reviews Departments
1. Login with **10002**
2. Land on UnitPresidentDashboard
3. See **9 department cards:**
   - 8 cards with green ✓ badge (Compliant)
   - 1 card with yellow ⚠ badge (Finance - needs review)
4. Scroll to **Compliance Summary:**
   - 8 Compliant, 1 Needs Review, 0 Pending
5. See **Timeline** showing progression:
   - Draft (Jan 20)
   - Submitted (Jan 28)
   - Awaiting Presidential Review (Today)
   - Closes Feb 7
6. Type in **Presidential Review Notes** textarea
7. Click **"Approve & Forward"** button (navigates to state dashboard)

### Test 3: State GS Monitors Units
1. Login with **20001**
2. Land on StateGSDashboard
3. See **4 stat cards at top:**
   - Units in State: 12
   - Reports Submitted: 11
   - Pending Submissions: 1
   - Compliant: 9
4. Scroll to **Units Table** with 12 rows:
   - Unit name, President name
   - Submission status (submitted/pending badge)
   - Compliance % bar (e.g., 8/9 = 89%)
   - Review button for each
5. Sort/filter by status (not implemented, but shows structure)

### Test 4: National Views System Overview
1. Login with **30001** (National GS)
2. Land on NationalDashboard
3. See **4 big stat cards:**
   - States: 36
   - Units Submitted: 450
   - Needs Attention: 50
   - Compliance Rate: 92%
4. Scroll to **Escalations Section** - 4 states with issues:
   - Oyo State (Financial crisis)
   - Cross River (Leadership transition)
   - Bauchi (Participation concerns)
   - Rivers (Compliance issues)
5. See **Submission Timeline:**
   - Opened (Jan 20)
   - 450 Submitted (Jan 28)
   - Currently Reviewing (Today)
   - Deadline (Feb 7)
6. See **Top 3 Performing States:**
   - Lagos: 98% compliant
   - Ogun: 96% compliant
   - Kaduna: 94% compliant

### Test 5: Navigation & Logout
1. From any dashboard, click **"AMSA Reporting"** logo → go to Dashboard
2. Click **user dropdown** (top-right) → shows first name + role
3. Click **"Logout"** → redirected to Login page
4. Try to access `/dashboard` directly → redirected to Login (auth check)

---

## Form Compliance Demo

### Taleem Report - Compliance Rules
```
✓ COMPLIANT if:
  - Sessions Organized ≥ 4 AND
  - Attendance Percentage ≥ 75%
```

### Test Cases

**Case 1: Fully Compliant**
- Sessions: 5
- Total Members: 40
- Average Attendance: 30 (75%)
- Result: ✓ Compliant (green badges)

**Case 2: Low Sessions**
- Sessions: 2
- Total Members: 40
- Average Attendance: 35 (87.5%)
- Result: ⚠ Sessions below threshold (red badge)
- Overall: ⚠ NOT COMPLIANT

**Case 3: Low Attendance**
- Sessions: 4
- Total Members: 50
- Average Attendance: 30 (60%)
- Result: ⚠ Attendance below 75% (red badge)
- Overall: ⚠ NOT COMPLIANT

**Case 4: Both Missing**
- Sessions: 1
- Total Members: 40
- Average Attendance: 20 (50%)
- Result: ⚠ Both below threshold (red badges)
- Overall: ⚠ NOT COMPLIANT

---

## Dashboard Role Routing

```
@page "/dashboard"
↓
Dashboard.razor checks CurrentUser roles
↓
if IsDepartmentOfficer          → DepartmentOfficerDashboard
else if IsUnitPresident         → UnitPresidentDashboard
else if IsStateGS/IsStatePresident → StateGSDashboard
else if IsNationalLeadership    → NationalDashboard
else                            → "Role not configured" alert
```

**Computed Properties** (in AuthContext):
```csharp
public bool IsDepartmentOfficer => Roles.Any(r => r.EndsWith(":unit") && !r.StartsWith("President"));
public bool IsUnitPresident => Roles.Contains("President:unit");
public bool IsStateGS => Roles.Contains("GeneralSecretary:state");
public bool IsStatePresident => Roles.Contains("President:state");
public bool IsNationalLeadership => Roles.Any(r => r.EndsWith(":national"));
```

---

## Component Structure

```
Login.razor
└─ Injects: AmsaAuthStateProvider, NavigationManager
└─ On submit: Calls AuthStateProvider.LoginAsync()
   └─ Which calls MockAuthService.LoginAsync()
   └─ Which returns AuthContext with roles

Dashboard.razor
└─ Injects: AmsaAuthStateProvider
└─ OnInitialized: Gets CurrentUser via AuthStateProvider.GetCurrentUser()
└─ Routes to one of 4 dashboards based on role

DepartmentOfficerDashboard.razor
└─ [Parameter] CurrentUser: AuthContext
└─ Shows single assigned section
└─ OnClick "Edit Form": Navigates to /report/taleem

UnitPresidentDashboard.razor
└─ [Parameter] CurrentUser: AuthContext
└─ Shows 9 departments in grid
└─ Displays compliance badges based on mock status

StateGSDashboard.razor & NationalDashboard.razor
└─ Display analytics based on role

TaleemReport.razor
└─ @page "/report/taleem"
└─ Form with real-time compliance calculations
└─ OnChange: Debounced auto-save (1.5s delay)
└─ Shows "Saving..." spinner, then "Saved at HH:mm:ss"
```

---

## Common Tasks

### Change Test User Roles
Edit `AMSAReportingSystem\Services\MockAuthService.cs`:
```csharp
private void SeedMockUsers()
{
    MockUsers.AddOrUpdate("10001", new AuthContext
    {
        MemberId = 1,
        MkanId = "10001",
        FirstName = "Ahmad",
        LastName = "Hassan",
        UnitId = 1,
        Roles = new List<string> { "Taleem:unit", "read:members" } // ← Change roles here
    });
    // ... more users
}
```

Then restart the app and login with **10001**.

### Add New Dashboard Component
1. Create `Components/MyCustomDashboard.razor`
2. Add role check in `Dashboard.razor`:
```razor
else if (CurrentUser.IsMyCustomRole)
{
    <MyCustomDashboard CurrentUser="@CurrentUser" />
}
```
3. Add computed property to `AuthContext.cs`:
```csharp
public bool IsMyCustomRole => Roles.Contains("MyCustomRole:national");
```

### Change Form Compliance Rules
Edit `AMSAReportingSystem\Components\Pages\TaleemReport.razor` @code section:
```csharp
private bool IsSessionsCompliant => FormData.SessionsOrganized >= 4; // Change threshold
private bool IsAttendanceCompliant => (FormData.AverageAttendanceCount / (FormData.TotalMemberCount ?? 1)) * 100 >= 75; // Change %
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "404 Not Found" at localhost | Wrong port. Check app output for correct HTTPS port. |
| Login always fails | Check MockAuthService has MKAN IDs 10001, 10002, etc. seeded. |
| Dashboard shows blank | Check browser console for JS errors. Refresh (Ctrl+Shift+R). |
| Form not saving | Check browser console. Auto-save is simulated (not persisted). |
| Compliance badge not updating | Form change handler may not be firing. Try changing another field first. |
| Can't access /report/taleem | Must login first and be redirected via dashboard navigation. |

---

## What's Next

1. **Test all 5 user paths** - Verify each role sees correct dashboard
2. **Create remaining 8 department forms** - Copy TaleemReport.razor pattern
3. **Build backend API** - Replace MockAuthService with real endpoints
4. **Add persistence** - Connect to database instead of in-memory mock data
5. **Implement compliance engine** - Move validation to backend service
6. **Add report workflow** - State transitions, forwarding, audit logging

---

## Files Reference

### Key Files to Understand
- `Models/AuthModels.cs` - All data models (start here for data structure)
- `Services/MockAuthService.cs` - Auth simulation (5 test users hardcoded)
- `Services/AmsaAuthStateProvider.cs` - Blazor auth state management
- `Components/Pages/Login.razor` - Entry point
- `Components/Pages/Dashboard.razor` - Role routing logic
- `Components/Pages/TaleemReport.razor` - Form example with compliance

### Modified Files
- `Program.cs` - Added MockAuthService, AmsaAuthStateProvider, AuthorizationCore to DI
- `Components/_Imports.razor` - Added Authorization using directive

---

**Happy testing! 🎉**

For issues or questions, refer to `docs/FRONTEND_POC_SUMMARY.md` for detailed component descriptions.
