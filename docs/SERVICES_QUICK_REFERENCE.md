# AMSA Services - Quick Reference

## What's Where? 🗺️

### 📝 Need to Work on Reports?
→ **`UnifiedReportService.cs`**
- Department report save/retrieve
- State report operations
- Draft creation & submission workflow
- Reporting cycle management
- JSON validation

**Common methods:**
- `SaveDepartmentDataAsync()`
- `GetDepartmentReportAsync()`
- `GetStateReportAsync()`
- `SubmitReportToPresidentAsync()`
- `ApproveByUnitLeadershipAsync()`

---

### 📋 Need to Work on Report Forms?
→ **`FormModels.cs`**
- All 9 department form classes (Taleem, Tabligh, Welfare, Sport, Finance, Health, SecondarySchool, Tajneed, General)
- State forms
- Form mappers & serialization

**Common classes:**
- `DepartmentReportForm` (base)
- `StateReportForm` (base)
- `DepartmentReportFormMapper`
- `StateReportFormMapper`

---

### 🔐 Need to Check User Permissions?
→ **`ReportAccessService.cs`**
- `CanEditDepartment()` — Can user edit department?
- `CanInitiateReportSubmission()` — Can user submit?
- `CanReviewAtUnitLevel()` — Unit reviewer?
- `CanReviewAtStateLevel()` — State reviewer?

**Usage:**
```csharp
if (AccessService.CanEditDepartment(authContext))
{
    // Allow edit
}
```

---

### 👤 Need to Get Current User Data?
→ **`CurrentUserReportService.cs`**
- Wraps UnifiedReportService with current user context
- No need to pass auth manually
- Integrates with AMSAAuthStateProvider

**Common methods:**
- `EnsureActiveCycleAsync()`
- `EnsureCurrentUserDraftAsync()`
- `GetDepartmentAsync()`
- `SaveDepartmentJsonAsync()`
- `SubmitReportToPresidentAsync()`

---

### 🔑 Need to Authenticate with AMSA API?
→ **`AMSAAuthService.cs`**
- `AuthenticateAsync()` — Authenticate member
- `GenerateTokenAsync()` — Get JWT token
- `RefreshAsync()` — Refresh expired token
- `ParseRole()` — Extract role from response
- `DetermineDashboard()` — Get user dashboard

---

### 🎨 Need to Check Auth State in Blazor?
→ **`AMSAAuthStateProvider.cs`**
- Implements Blazor `AuthenticationStateProvider`
- `LoginAsync()` — Log user in
- `LogoutAsync()` — Log user out
- `GetAuthenticationStateAsync()` — For Blazor auth

**Usage:**
```csharp
@inject AuthenticationStateProvider AuthStateProvider
var state = await AuthStateProvider.GetAuthenticationStateAsync();
var principal = state.User;
```

---

### 📱 Need to Access User Identity/Roles?
→ **`AuthenticationContext.cs`**
- `GetDisplayName()` — User display name
- `GetDashboard()` — User dashboard assignment
- `IsDepartmentOfficer()` — Check if user is officer
- `IsUnitLeadership()` — Check unit leadership role
- `IsStateLeadership()` — Check state leadership role
- `IsNationalLeadership()` — Check national leadership role
- `HasSudoAccess()` — Check super-user status

---

### 🌐 Need to Make API Calls?
→ **`IAmSaApiClient.cs`** (contract) + **`AMSAApiClient.cs`** (implementation)

**Available endpoints:**
- `GenerateTokenAsync()` — Get authentication token
- `GetMemberByIdAsync()` — Fetch member by ID
- `GetMemberByMkanAsync()` — Fetch member by MKAN
- `GetAllStatesAsync()` — List all states
- `GetStateByIdAsync()` — Get state details
- `GetUnitsByStateAsync()` — List units in state
- `GetUnitByIdAsync()` — Get unit details
- `CreateAppAsync()` — Register new app
- `GetAppAsync()` — Fetch app registration

---

### ⚙️ Need to Configure AMSA API Client?
→ **`AMSAApiClientCore.cs`**

**Sections:**
- `AMSAApiClientOptions` — Endpoint URL, timeouts, default credentials
- `ResiliencePolicyOptions` — Retry policy, circuit breaker settings
- `AMSAApiConnectionStatus` — Singleton status object (for UI)
- `AMSAApiStartupHealthCheckService` — Auto health check on startup

**Configuration in `appsettings.json`:**
```json
{
  "AmSaApiClient": {
    "BaseUrl": "https://api.example.com",
    "TimeoutSeconds": 30,
    "DefaultUsername": "user",
    "DefaultPassword": "pass"
  }
}
```

---

### 💊 Need to Monitor API Connection?
→ **`AMSAApiConnectionStatus.cs`** (in `AMSAApiClientCore.cs`)
- `IsConnected` — Boolean status
- `LastError` — Most recent error message
- `LastChecked` — Timestamp of last health check
- `MarkConnected()` — Update on success
- `MarkFailed()` — Update on failure

**Usage in Blazor:**
```csharp
@inject AMSAApiConnectionStatus ApiStatus

<div class="@(ApiStatus.IsConnected ? "alert-success" : "alert-danger")">
    @(ApiStatus.IsConnected ? "API Connected" : "API Down")
</div>
```

---

## Service Dependency Graph

```
CurrentUserReportService
  └── UnifiedReportService
      ├── AMSAReportingDbContext (EF Core)
      ├── IAmSaApiClient (HTTP)
      └── ReportAccessService
          └── AuthenticationContext

AMSAAuthStateProvider
  ├── AMSAAuthService
  │   ├── IAmSaApiClient
  │   └── AuthenticationContext
  └── AuthenticationContext

Blazor Components
  ├── CurrentUserReportService
  ├── AMSAAuthStateProvider
  ├── ReportAccessService
  ├── AMSAApiConnectionStatus
  └── AuthenticationContext
```

---

## Common Patterns

### Inject & Use Report Service
```csharp
@inject CurrentUserReportService ReportService

@code {
    private async Task SaveReport()
    {
        var draft = await ReportService.EnsureCurrentUserDraftAsync();
        await ReportService.SaveDepartmentJsonAsync(draft.Id, jsonData);
    }
}
```

### Check Permission Before Action
```csharp
@inject ReportAccessService AccessService
@inject AuthenticationContext AuthContext

@code {
    private bool CanEdit => AccessService.CanEditDepartment(AuthContext);
}
```

### Authenticate User
```csharp
@inject AMSAAuthStateProvider AuthProvider

@code {
    private async Task LoginUser()
    {
        var authContext = await AuthProvider.LoginAsync(username, password);
        if (authContext != null)
        {
            // Login successful
        }
    }
}
```

---

## Statistics

- **Total Services:** 11 files
- **Lines of Code:** ~4,500
- **Key Classes:** 35+
- **Public Methods:** 100+

---

**Last Updated:** After consolidation refactoring session  
**Version:** 1.0
