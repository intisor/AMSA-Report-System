# AMSA API Client Configuration - Setup & Next Steps

## ✅ Completed (This Session)

### 1. **Configuration**
- ✅ Updated `appsettings.json` with AMSA API settings
- ✅ Created `AmSaApiClientOptions.cs` for strongly-typed configuration binding
- ✅ Registered configuration in DI container

### 2. **Client Interface & DTOs**
- ✅ Created `IAmSaApiClient` interface with 9 methods
  - GenerateTokenAsync
  - GetMemberByIdAsync
  - GetMemberByMkanAsync
  - GetAllStatesAsync
  - GetStateByIdAsync
  - GetUnitsByStateAsync
  - GetUnitByIdAsync
  - CreateAppAsync
  - GetAppAsync

- ✅ Created all response DTOs:
  - `TokenResponse`
  - `MemberResponse` + `OrganizationHierarchyDto`
  - `RoleDto`
  - `StateResponse`
  - `UnitResponse` + `MemberSummaryDto`
  - `AppRegistrationResponse`
  - `CreateAppRequest`
  - `ErrorResponse`
  - `Result<T>` (generic wrapper for success/failure)

### 3. **HTTP Client Implementation**
- ✅ Created `AmSaApiClient.cs` with full implementation
- ✅ Registered as typed HttpClient in DI
- ✅ Added structured logging for all operations
- ✅ Proper error handling and JSON deserialization

### 4. **Authentication Service**
- ✅ Created `AmSaAuthService` to replace MockAuthService
- ✅ Implements app-to-app authentication flow (AMSA API model)
- ✅ Token generation and member profile fetch
- ✅ Token caching for performance

### 5. **Auth State Provider**
- ✅ Updated `AmsaAuthStateProvider` to use real AmSaAuthService
- ✅ Changed from password-based to MKAN ID-only authentication
- ✅ Maps AMSA member data to Blazor ClaimsPrincipal

### 6. **Helper Methods**
- ✅ Added role helpers to `AuthContext`:
  - `IsAuthenticated`
  - `IsDepartmentOfficer`
  - `IsUnitPresident`
  - `IsStateGS`
  - `IsStatePresident`
  - `IsNationalGS`
  - `IsNationalLeadership`
  - `HasRole(string)`
  - `GetDisplayName()`
  - `FullName`

### 7. **Build Status**
- ✅ **Build successful** - All code compiles without errors

---

## ⏳ Next Steps (Implementation Ready)

### Step 1: Configure AppSecret (One-Time Setup)
**What to do:** Get from AMSA API administrator

1. Contact AMSA API admin or technical lead
2. Request: Register "ReportingApp" with AMSA API
3. Get back: `AppSecret` (e.g., "your-app-secret-key-here")
4. Store securely:
   ```bash
   # Development (using User Secrets)
   cd AMSAReportingSystem
   dotnet user-secrets set "AmSaApi:AppSecret" "your-app-secret-key-here"
   
   # Production (via environment variable or secure config)
   export AMSAAPI_APPSECRET="your-app-secret-key-here"
   ```

### Step 2: Update appsettings.json

**File:** `AMSAReportingSystem/appsettings.json`

```json
{
  "AmSaApi": {
    "BaseUrl": "https://api-dev.amsa.ng",  // or staging/prod as needed
    "AppId": "ReportingApp",
    "AppSecret": "${AMSAAPI_APPSECRET}",   // Will be replaced by User Secrets or env var
    "RequestTimeoutSeconds": 30
  }
}
```

### Step 3: Test Connectivity

```csharp
// Add this to Program.cs temporarily for testing
var app = builder.Build();

// Test AMSA API connection on startup
var apiClient = app.Services.GetRequiredService<IAmSaApiClient>();
var testResult = await apiClient.GetAllStatesAsync();
if (!testResult.IsSuccess)
{
    Console.WriteLine($"⚠️ WARNING: Could not connect to AMSA API: {testResult.ErrorMessage}");
}
else
{
    Console.WriteLine($"✅ Connected to AMSA API - {testResult.Data?.Count} states loaded");
}
```

### Step 4: Test Login Flow

**File:** `AMSAReportingSystem/Components/Pages/Login.razor`

Current implementation:
- User enters MKAN ID (no password needed for app-to-app model)
- System calls AmSaAuthService.AuthenticateAsync(mkanId)
- Service calls AMSA API: `/api/auth/token` with app credentials
- Gets JWT token + member details
- Creates AuthContext with roles and hierarchy
- Redirects to Dashboard

To test:
1. Run the application
2. Navigate to `/login`
3. Enter test MKAN ID (get from AMSA API test credentials or admin)
4. Should see:
   - Token generated from AMSA API
   - Member details fetched
   - Redirect to dashboard showing member info

### Step 5: Update Reference Data Sync (Future)

Create a background service to cache states/units locally:

```csharp
// Pseudocode for reference data sync service
public class AmSaReferenceDataService : BackgroundService
{
    private readonly IAmSaApiClient _apiClient;
    private readonly IMemoryCache _cache;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var states = await _apiClient.GetAllStatesAsync(ct);
                if (states.IsSuccess)
                {
                    _cache.Set("amsa:states", states.Data, 
                        new MemoryCacheEntryOptions 
                        { 
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) 
                        });
                }
            }
            catch (Exception ex)
            {
                // Log error
            }
            
            await Task.Delay(TimeSpan.FromHours(24), ct);
        }
    }
}
```

---

## 📋 Configuration Checklist

- [ ] Get AppSecret from AMSA API administrator
- [ ] Store AppSecret in User Secrets (dev) or environment variable (prod)
- [ ] Verify BaseUrl matches environment (dev/staging/prod)
- [ ] Test connectivity to AMSA API
- [ ] Test login flow with real MKAN ID
- [ ] Verify member data displays correctly on Dashboard
- [ ] Verify role-based nav menu items appear

---

## 🔧 Files Created/Modified

**New Files:**
- `AMSAReportingSystem/Services/AmSaApiClientOptions.cs` - Configuration classes
- `AMSAReportingSystem/Services/IAmSaApiClient.cs` - Interface + all DTOs
- `AMSAReportingSystem/Services/AmSaApiClient.cs` - HTTP client implementation
- `AMSAReportingSystem/Services/AmSaAuthService.cs` - Authentication service

**Modified Files:**
- `AMSAReportingSystem/appsettings.json` - Added AmSaApi section
- `AMSAReportingSystem/Program.cs` - Registered AmSaApiClient and AmSaAuthService
- `AMSAReportingSystem/Services/AmsaAuthStateProvider.cs` - Updated to use AmSaAuthService
- `AMSAReportingSystem/Services/MockAuthService.cs` - Updated to match new AuthContext shape

**Deleted Files:**
- `AMSAReportingSystem/Models/AuthModels.cs` - Moved to Services/AmSaAuthService.cs

---

## 🚀 Environment-Specific Configuration

### Development
```json
{
  "AmSaApi": {
    "BaseUrl": "https://api-dev.amsa.ng",
    "AppId": "ReportingApp",
    "AppSecret": "...", // From User Secrets
    "RequestTimeoutSeconds": 30
  }
}
```

### Staging
```json
{
  "AmSaApi": {
    "BaseUrl": "https://api-staging.amsa.ng",
    "AppId": "ReportingApp",
    "RequestTimeoutSeconds": 30
  }
}
```

### Production
```json
{
  "AmSaApi": {
    "BaseUrl": "https://api.amsa.ng",
    "AppId": "ReportingApp",
    "RequestTimeoutSeconds": 30
  }
}
```

---

## 📚 Reference Links

- AMSA API Integration Contract: `docs/AMSA_API_INTEGRATION_CONTRACT.md`
- Endpoint specifications: 9 endpoints documented with real request/response examples
- Authentication flow: App-to-app model (ReportingApp calls AMSA API)
- Role format: "DepartmentName:LevelType" (e.g., "Taleem:DepartmentOfficer")

---

## 💡 Next Phase: Additional Features

After basic login is working:

1. **Reference Data Caching** - Cache states/units locally with 24h TTL
2. **Resilience Policies** - Add Polly retry + circuit breaker
3. **Authorization Handlers** - Map AMSA roles to Reporting System permissions
4. **REST API Endpoints** - Create backend endpoints for reporting workflows
5. **Database Migrations** - Apply EF Core migrations (ready from Session 2)
6. **Report Submission** - Build forms for Taleem/Tabligh/Welfare reports

---

**Build Status:** ✅ Successful  
**Session:** Configuration Phase Complete  
**Next Action:** Get AppSecret and test real connectivity
