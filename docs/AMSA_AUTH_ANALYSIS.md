# AMSA API Authentication Analysis

## Current AMSA API Auth Flow

### 1. Token Generation Endpoint
**POST /api/auth/token** (AllowAnonymous)

**Required Request Fields:**
```csharp
public class TokenGenerationRequest
{
    public string? AppId { get; set; }           // App identifier (e.g., "ReportingApp")
    public string? AppName { get; set; }         // Alternative to AppId
    public required string AppSecret { get; set; } // Plain app secret (NOT hash!)
    public int MkanId { get; set; }              // **CRITICAL**: Member MKAN ID required
    public required string[] RequestedScopes { get; set; } // ["read:members", "read:organization"]
}
```

### 2. Token Generation Process (TokenService.GenerateTokenAsync)

1. **Validate Scopes**
   - Checks if requested scopes are in the ScopeDefinitions.AllValidScopes list
   - Valid scopes: read:members, export:members, verify:membership, read:organization, read:statistics, read:exco

2. **Find AppRegistration** (from database)
   - Lookup by AppId OR AppName
   - Returns 404 if app not found
   - Returns 400 if app is inactive (IsActive = false)

3. **Verify App Exists**
   - AppRegistration must exist in DB with:
     - AppId (unique key)
     - AppSecretHash (hashed version of secret - **NOT compared directly**)
     - AllowedScopes (JSON array of scopes app can request)
     - TokenExpirationHours (defaults to 1 hour if null)

4. **Load Member by MkanId**
   - Query Members table for member with matching MKAN ID
   - Returns 404 if member not found
   - **This is why requests fail: MkanId is required but reporting client doesn't send it**

5. **Load Member Roles** 
   - Query MemberLevelDepartments to get roles
   - Only included in JWT if requested scopes require roles

6. **Build JWT Claims**
   ```csharp
   new Claim("sub", member.MemberId.ToString()),
   new Claim("mkanId", member.Mkanid.ToString()),
   new Claim("firstName", member.FirstName),
   new Claim("lastName", member.LastName),
   new Claim("email", member.Email ?? string.Empty),
   new Claim("unitId", member.UnitId.ToString()),
   new Claim("app", appId),
   new Claim("isMember", "true"),
   new Claim("scope", scope) // for each granted scope
   ```

7. **Create JWT Token**
   - Signed with HS256 using Jwt:SecretKey from configuration
   - Issuer: from Jwt:Issuer config
   - Audience: appId (must be in ValidAudiences list during validation)
   - Expiry: DateTime.UtcNow.AddHours(expirationHours)

### 3. AMSA API Startup Configuration

From Program.cs:

```csharp
// JWT Configuration
var secretKey = builder.Configuration["Jwt:SecretKey"]; // Must be 32+ chars

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "AmsaAPI", // Expected issuer

            ValidateAudience = true,
            ValidAudiences = ["ReportingApp", "EventsApp", "PaymentApp"],

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

// Seeded ReportingApp registration:
new AppRegistration
{
    AppId = "ReportingApp",
    AppName = "AMSA Reporting Application",
    AppSecretHash = "test-secret-123-hash",
    AllowedScopes = "[\"read:members\", \"read:statistics\", \"read:organization\"]",
    TokenExpirationHours = 2,
    IsActive = true
}
```

## Current Reporting Client Status

The earlier token-generation issues have now been addressed in the client:

1. `EnsureValidTokenAsync` uses `ServiceAccountMkanId` instead of passing `AppId` as the MKAN value.
2. Token requests now use AMSA-valid scopes: `read:members` and `read:organization`.
3. The client caches the generated token and attaches it to authenticated requests.

Remaining dependency: `ServiceAccountMkanId` must point to a real member record in the AMSA API database, or token generation will still fail with a member lookup error.

## Solution Strategy

### For Reporting Client:

1. **Keep `ServiceAccountMkanId` valid**
   - It must reference an actual AMSA API member
   - If the member is missing, token generation will fail even though the request shape is now correct

2. **Preserve AMSA-valid scopes**
   - The client should continue using `read:members` and `read:organization`
   - Avoid reintroducing legacy scope names like `units:read` or `members:read`

3. **Keep token failure logging explicit**
   - Log the lookup failure if the service account member cannot be resolved
   - Do not silently switch to a local fallback token path

### For AMSA API (if needed):

1. **Implement AppSecret validation** (currently missing)
   - Hash incoming secret and compare to AppSecretHash
   - Return 401 if mismatch

2. **Allow app-level tokens** (optional)
   - Add option to generate token without MkanId
   - Use app's own AppId as subject instead of member
   - Useful for service-to-service calls

3. **Add custom scopes for Reporting App**
   - Create "submit:reports" scope
   - Create "read:units" scope if needed
   - Document scope hierarchy

## Key Constants from AMSA API

**Valid Scopes:**
- `read:members` (MemberScopesModule)
- `export:members` (MemberScopesModule)
- `verify:membership` (MemberScopesModule)
- `read:organization` (OrganizationScopesModule)
- `read:statistics` (AnalyticsScopesModule)
- `read:exco` (AnalyticsScopesModule)

**Seeded AppRegistration:**
- AppId: `ReportingApp`
- AppSecret: `test-secret-123` (from hash value stored)
- AllowedScopes: `["read:members", "read:statistics", "read:organization"]`
- TokenExpirationHours: 2
- Audience (in JWT validation): `ReportingApp`

## Why You're Getting 401

1. Token generation will fail if `ServiceAccountMkanId` does not map to a real AMSA member.
2. If token generation fails, the client may cache nothing and downstream calls will be unauthorized.
3. API requests then carry no valid bearer token and the server rejects them with 401 Unauthorized.

The current root cause to watch is no longer the request shape; it is whether the configured service-account MKAN ID is valid in the AMSA API database.
