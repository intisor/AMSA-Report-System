# AMSA API → Reporting System Integration Contract

## ✅ **ACTUAL ENDPOINTS AVAILABLE** (NOT ASSUMPTIONS)

Based on analysis of your real AmsaAPI codebase.

---

### **Authentication & Token Management**

#### 1️⃣ Generate Token for External App
```
POST /api/auth/token
Content-Type: application/json
AllowAnonymous()

Request:
{
  "appId": "ReportingApp",
  "appSecret": "your-app-secret",
  "mkanId": "10001",
  "requestedScopes": ["read:reports", "write:reports"]
}

Response (200 OK):
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer"
}

Notes:
- Token generated for a specific AppId + MemberId combo
- Scopes intersected with AppRegistration allowed scopes
- JWT includes: memberId, mkanId, firstName, lastName, email, unitId, roles, scopes
- Token expiration configured per app (default 1 hour)
- No refresh token endpoint available yet
```

#### 2️⃣ Create App Registration
```
POST /api/auth/apps
Authorization: Bearer <adminToken>
Roles: Admin

Request:
{
  "appId": "ReportingApp",
  "appName": "AMSA Reporting System",
  "appSecretHash": "hashed-secret",
  "allowedScopes": ["read:reports", "write:reports"],
  "tokenExpirationHours": 24
}

Response (201 Created):
{
  "appId": "ReportingApp",
  "appName": "AMSA Reporting System"
}
```

#### 3️⃣ Get App Details
```
GET /api/auth/apps/{appId}
Authorization: Bearer <adminToken>
Roles: Admin

Response (200 OK):
{
  "appId": "ReportingApp",
  "appName": "AMSA Reporting System",
  "isActive": true,
  "createdAt": "2026-01-19T00:00:00Z",
  "lastUsedAt": "2026-01-22T15:30:00Z"
}
```

---

### **Member Data & Roles**

#### 4️⃣ Get Member by ID (with full details + roles)
```
GET /api/members/{memberId}
AllowAnonymous()

Response (200 OK):
{
  "memberId": 10001,
  "firstName": "Ahmed",
  "lastName": "Hassan",
  "email": "ahmed@example.com",
  "phone": "+234-XXX-XXXX",
  "mkanid": 10001,
  "unit": {
    "unitId": 1,
    "unitName": "Ilorin Unit",
    "state": {
      "stateId": 24,
      "stateName": "Kwara",
      "national": {
        "nationalId": 1,
        "nationalName": "Nigeria"
      }
    }
  },
  "roles": [
    {
      "departmentName": "Taleem",
      "levelType": "DepartmentOfficer"
    },
    {
      "departmentName": null,
      "levelType": "UnitPresident"
    }
  ]
}
```

#### 5️⃣ Get Member by MKAN ID (with full details + roles)
```
GET /api/members/mkan/{mkanid}
AllowAnonymous()

Response (200 OK):
{
  ... same structure as above ...
}
```

---

### **Organization Hierarchy**

#### 6️⃣ Get All States
```
GET /api/states
AllowAnonymous()

Response (200 OK):
{
  "value": [
    {
      "stateId": 1,
      "stateName": "Abia",
      "nationalName": "Nigeria",
      "unitCount": 8,
      "memberCount": 250,
      "excoCount": 45
    },
    {
      "stateId": 24,
      "stateName": "Kwara",
      "nationalName": "Nigeria",
      "unitCount": 12,
      "memberCount": 450,
      "excoCount": 78
    }
    // ... 35 more states
  ]
}
```

#### 7️⃣ Get State by ID
```
GET /api/states/{stateId}
AllowAnonymous()

Response (200 OK):
{
  "stateId": 24,
  "stateName": "Kwara",
  "nationalName": "Nigeria",
  "unitCount": 12,
  "memberCount": 450,
  "excoCount": 78
}
```

#### 8️⃣ Get All Units in a State
```
GET /api/units/state/{stateId}
AllowAnonymous()

Response (200 OK):
{
  "value": [
    {
      "unitId": 1,
      "unitName": "Ilorin Unit",
      "memberCount": 45,
      "excoCount": 12
    },
    {
      "unitId": 2,
      "unitName": "Offa Unit",
      "memberCount": 32,
      "excoCount": 8
    }
    // ... 10 more units
  ]
}
```

#### 9️⃣ Get Unit by ID (with members & EXCO)
```
GET /api/units/{unitId}
AllowAnonymous()

Response (200 OK):
{
  "unit": {
    "unitId": 1,
    "unitName": "Ilorin Unit",
    "stateId": 24,
    "presidentName": "Ahmed Hassan",
    "generalSecretaryName": "Fatima Musa"
  },
  "members": [
    {
      "memberId": 10001,
      "firstName": "Ahmed",
      "lastName": "Hassan",
      "email": "ahmed@example.com",
      "phone": "+234-XXX-XXXX",
      "mkanid": 10001
    }
    // ... 44 more members
  ],
  "excoRoles": [
    {
      "firstName": "Ahmed",
      "lastName": "Hassan",
      "mkanid": 10001,
      "departmentName": "Taleem",
      "levelType": "DepartmentOfficer"
    }
    // ... more EXCO members
  ]
}
```

---

## 🔑 **Key Integration Points for Reporting System**

### **Authentication Flow (Current)**
```
1. App-to-App: Reporting System calls AMSA API with:
   - appId: "ReportingApp"
   - appSecret: [registered secret]
   - mkanId: [user's MKAN]
   - requestedScopes: ["read:reports", "write:reports"]

2. AMSA API returns JWT token

3. Reporting System stores JWT and uses it for subsequent calls

4. When token expires: User must re-login (no refresh endpoint)
```

### **User Profile Access**
```
With JWT token, can call:
- GET /api/members/{memberId}  → Get user + roles
- GET /api/members/mkan/{mkanid} → Get user + roles by MKAN
```

### **Reference Data Caching**
```
All read-only, can cache 24h:
- GET /api/states              → Cache all 37 states
- GET /api/units/state/{id}    → Cache units per state
- GET /api/units/{id}          → Cache full unit details
```

---

## ⚠️ **What's MISSING vs. Spec I Proposed**

| Feature | Your API | Status |
|---------|----------|--------|
| User login (MKAN + password) | ❌ No endpoint | Must call app-to-app token |
| Token refresh | ❌ No endpoint | Token expiration = forced re-login |
| Authorization checks | ❌ No endpoint | Check roles locally from JWT claims |
| Rate limiting | ❌ Not implemented | Add if needed |
| Error standardization | ⚠️ Mixed | FastEndpoints handles it |

---

## 🎯 **What Reporting System Needs To Do**

### **1. Create AppRegistration in AMSA API (One-Time)**
```bash
# Register ReportingApp with AMSA API admin
POST /api/auth/apps
{
  "appId": "ReportingApp",
  "appName": "AMSA Reporting System",
  "appSecretHash": "hashed-secret",
  "allowedScopes": ["read:reports", "write:reports"],
  "tokenExpirationHours": 24
}

Response:
appId: "ReportingApp"
appSecret: [save this securely in appsettings]
```

### **2. Replace MockAuthService with Real Flow**
```csharp
// OLD (MockAuthService):
public async Task<AuthResult> LoginAsync(string mkanId, string password)
{
    // Hardcoded test users
    return new AuthResult { Success = true, User = testUser };
}

// NEW (AmSaAuthService):
public async Task<AuthResult> LoginAsync(string mkanId, string password)
{
    // Call AMSA API:
    var tokenResponse = await _httpClient.PostAsync("/api/auth/token", 
    {
        appId: "ReportingApp",
        appSecret: _config["AmSa:AppSecret"],
        mkanId: mkanId,
        requestedScopes: ["read:reports", "write:reports"]
    });
    
    // Get JWT from response
    var token = tokenResponse.token;
    
    // Call AMSA API to get user details:
    var userResponse = await _httpClient.GetAsync(
        $"/api/members/mkan/{mkanId}",
        headers: { Authorization: $"Bearer {token}" }
    );
    
    // Create AuthContext from response
    var user = new AuthContext
    {
        MemberId = userResponse.memberId,
        FirstName = userResponse.firstName,
        Roles = userResponse.roles.Select(r => r.departmentName ?? "UnitPresident").ToList()
    };
    
    return new AuthResult { Success = true, User = user, Token = token };
}
```

### **3. Cache Reference Data**
```csharp
public class AmSaReferenceDataService
{
    // On app startup:
    await SyncStatesAsync();  // GET /api/states → cache all 37
    
    // On background job (24h interval):
    await RefreshStatesAndUnitsAsync();
}
```

### **4. Use JWT for User Authorization**
```csharp
// From JWT claims:
var memberId = context.User.FindFirst("sub")?.Value;
var roles = context.User.FindAll("role");

// Check if user is DepartmentOfficer:
var isDepartmentOfficer = roles.Any(r => r.Value.Contains("DepartmentOfficer"));
```

---

## 📦 **What I Need To Build**

### **For Reporting System Integration:**

1. **`IAmSaApiClient` interface**
   - Token generation
   - Member data fetching
   - Reference data queries

2. **`AmSaApiClient` HTTP client**
   - Calls actual AMSA endpoints
   - Handles JWT token injection
   - Error handling + retries

3. **`AmSaAuthService` (replaces MockAuthService)**
   - App-to-app token generation
   - User details fetch via JWT
   - Role mapping

4. **`AmSaReferenceDataService`**
   - Sync states/units from AMSA
   - Cache locally with TTL
   - Background refresh job

5. **DTOs for responses**
   - `MemberDetailResponse`
   - `StateDto`, `UnitDto`
   - `TokenResponse`

6. **Authorization handler**
   - Map AMSA roles to Reporting System roles
   - Validate permissions

---

## ✅ **Next Steps**

1. **Get AppSecret from AMSA API admin**
   - Register "ReportingApp"
   - Save secret in appsettings.json

2. **I build the client layer**
   - AmSaApiClient (typed HTTP client)
   - AmSaAuthService (real login flow)
   - Reference data sync service

3. **Wire it into Reporting System**
   - Replace MockAuthService
   - Update Program.cs DI
   - Add background job for sync

4. **Test it**
   - Login with real AMSA user
   - Verify JWT token valid
   - Check role mapping
   - Confirm reference data cached

---

## 📝 **Config Needed**

Add to `appsettings.json`:

```json
{
  "AmSa": {
    "BaseUrl": "https://api-dev.amsa.ng",
    "AppId": "ReportingApp",
    "AppSecret": "your-registered-secret",
    "AllowedScopes": ["read:reports", "write:reports"],
    "TokenExpirationMinutes": 60,
    "ReferenceDataCacheTTLHours": 24
  }
}
```

---

**Ready?** I can now build the actual integration layer! 🚀
