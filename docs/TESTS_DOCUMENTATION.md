# AMSA Reporting System — Test Documentation

## Overview
This document describes all current automated tests in the repository, including unit tests and integration tests.

- **Unit test project:** `AMSAReportingSystem.Tests`
- **Integration test project:** `AMSAReportingSystem.IntegrationTests`
- **Framework:** xUnit
- **Mocking library:** Moq
- **Host integration utility:** `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`)

---

## Test Projects

### 1) `AMSAReportingSystem.Tests`
Purpose: Validate deterministic business logic and helper behavior without external infrastructure dependencies.

### 2) `AMSAReportingSystem.IntegrationTests`
Purpose: Validate host bootstrapping and route-level HTTP behavior through the real ASP.NET Core pipeline.

---

## Unit Tests (AMSAReportingSystem.Tests)

### `ReportAccessServiceTests`
**Target:** `ReportAccessService`

#### Covered tests
- `CanEditDepartment_WhenNationalLeadership_ReturnsTrue`
- `CanEditDepartment_WhenStateLeadershipOnOwnState_ReturnsTrue`
- `CanEditDepartment_WhenUnitLeadershipOnOwnUnit_ReturnsTrue`
- `CanEditDepartment_WhenDepartmentMatchAtUnitLevel_ReturnsTrue`
- `CanEditDepartment_WhenNoRuleMatches_ReturnsFalse`
- `CanInitiateReportSubmission_WhenNationalLeadership_ReturnsTrue`
- `CanInitiateReportSubmission_WhenSameUnitAndStateWithUnitRole_ReturnsTrue`
- `CanInitiateReportSubmission_WhenOutsideScope_ReturnsFalse`
- `CanReviewAtUnitLevel_WhenUnitLeaderOnOwnUnit_ReturnsTrue`
- `CanReviewAtStateLevel_WhenStateLeaderOnOwnState_ReturnsTrue`

**Behavior validated**
- Authorization logic across national, state, and unit levels.
- Department-based access scope.
- Submission and review permissions.

---

### `AmsaTokenCacheTests`
**Target:** `AmsaTokenCache`

#### Covered tests
- `GetValidToken_WhenTokenIsFresh_ReturnsToken`
- `GetValidToken_WhenTokenIsExpired_ReturnsNull`
- `SetMkanId_StoresTheValue`
- `Clear_RemovesCachedToken`
- `CalculateEffectiveExpiration_WhenInputIsUtc_ReturnsEarlierUtcCutoff`

**Behavior validated**
- Token freshness and expiry handling.
- MKAN persistence.
- Cache clearing.
- Effective expiration safety-skew logic.

---

### `AmsaDirectoryLookupCacheTests`
**Target:** `AmsaDirectoryLookupCache`

#### Covered tests
- `GetStateNameAsync_WhenStateExists_ReturnsStateName`
- `GetStateNameAsync_WhenCached_ReturnsCachedValue`
- `GetUnitNameAsync_WhenLookupFails_ReturnsFallbackName`

**Behavior validated**
- State name resolution and memoization.
- Fallback behavior for failed unit lookup.

---

### `DepartmentReportFormMapperTests`
**Target:** `DepartmentReportFormMapper`

#### Covered tests
- `Deserialize_WhenJsonIsEmpty_ReturnsDefaultTaleemForm`
- `Deserialize_WhenTablighJsonProvided_ReturnsTablighForm`
- `Serialize_WhenTablighFormHasCampusText_PersistsDerivedFlag`
- `Serialize_WhenTablighFormHasNoActivities_ReturnsDerivedFlagFalse`
- `Deserialize_WhenInvalidJson_ReturnsDefaultFormInstance`

**Behavior validated**
- Department-specific deserialization type mapping.
- Fault-tolerant deserialization for invalid payloads.
- Tabligh derived-field normalization during serialization.

---

### `StateReportFormMapperTests`
**Target:** `StateReportFormMapper`

#### Covered tests
- `ToForm_WhenStateReportProvided_MapsCoreProperties`

**Behavior validated**
- Entity-to-form projection.
- Program ordering and key field mapping.

---

### `AuthContextTests`
**Target:** `AuthContext`

#### Covered tests
- `GetDisplayName_ReturnsFirstAndLastName`
- `GetDashboard_WhenNationalDashboardPresent_ReturnsNationalDashboard`
- `IsNationalLeadership_WhenNationalRoleExists_ReturnsTrue`
- `HasSudoAccess_WhenNoLeadershipRoleExists_ReturnsFalse`

**Behavior validated**
- Display helpers.
- Dashboard selection precedence.
- Leadership and elevated access checks.

---

### `ReportDepartmentCatalogTests`
**Target:** `ReportDepartmentCatalog`

#### Covered tests
- `ReportableDepartments_ContainsAllDepartmentsInExpectedOrder`

**Behavior validated**
- Department inclusion and ordering contract.

---

### `EntityAndDtoTests`
**Targets:** `StateReport`, `ReportWithStateContext`

#### Covered tests
- `UnitPresidentAttendanceRate_WhenTotalIsZero_ReturnsZero`
- `ReportWithStateContext_CanBeCreated`

**Behavior validated**
- Computed property zero-denominator behavior.
- DTO construction contract.

---

## Integration Tests (AMSAReportingSystem.IntegrationTests)

### `TestAppFactory`
**Target:** ASP.NET Core host startup via `WebApplicationFactory<Program>`

**Purpose**
- Bootstraps the full web host for HTTP-level integration tests.

---

### `HostSmokeTests`
**Targets:** Host + route availability

#### Covered tests
- `GetRootPage_ReturnsSuccessfulResponse`
- `GetLoginPage_ReturnsSuccessfulResponse`

**Behavior validated**
- Application host starts successfully.
- Root and login routes respond with success status.

---

### `RoutingTests`
**Targets:** Routing behavior for unknown paths

#### Covered tests
- `GetUnknownRoute_ReturnsClientHandledResponse`

**Behavior validated**
- Unknown route request returns a valid HTTP response object through the app pipeline.

---

## Current Test Inventory Summary

- **Unit test files:** 8
- **Integration test files:** 3 (plus shared global usings)
- **Total tests:** 32

---

## How to Run Tests

### Run unit tests
```powershell
dotnet test .\AMSAReportingSystem.Tests\AMSAReportingSystem.Tests.csproj
```

### Run integration tests
```powershell
dotnet test .\AMSAReportingSystem.IntegrationTests\AMSAReportingSystem.IntegrationTests.csproj
```

### Run all tests
```powershell
dotnet test
```

---

## Notes

- The existing tests are intentionally focused on deterministic logic and host smoke coverage.
- Integration coverage can be expanded with authenticated flows and deeper route/content assertions.
- Related specification reference: `docs/UNIT_TEST_SPEC.md`.
